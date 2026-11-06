using Microsoft.EntityFrameworkCore;
using BookNest.Data;
using BookNest.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// MSSQL Bağlantısını Ekleme
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// E-posta onayı kapalı. Gevşek şifre kuralları sadece geliştirme ortamında geçerli
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZçğıöşüÇĞİÖŞÜ0123456789-._@+ ";

    if (builder.Environment.IsDevelopment())
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 3;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
    }
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Flutter uygulaması için /api/... adresleri

// Flutter'ı Chrome'da çalıştırırsan farklı bir porttan istek atar, ona izin ver
builder.Services.AddCors(options =>
{
    options.AddPolicy("FlutterApp", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddHttpClient<GoogleBooksService>();

var app = builder.Build();

// Veritabanını hazırla: tablolar, admin hesabı ve kitaplar.json'daki kitaplar
await VeritabaniHazirlik.HazirlaAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// /api isteklerini HTTPS'e yönlendirme: emülatör localhost sertifikasına güvenmez
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseHttpsRedirection());

app.UseCors("FlutterApp");
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.Run();
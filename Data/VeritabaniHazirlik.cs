using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Data
{
    // Program her açıldığında veritabanını kullanıma hazırlar.
    // Projeyi GitHub'dan çeken herkes sadece "dotnet run" der, gerisini bu dosya yapar.
    public static class VeritabaniHazirlik
    {
        public static async Task HazirlaAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            var db = services.GetRequiredService<AppDbContext>();
            var config = services.GetRequiredService<IConfiguration>();
            var env = services.GetRequiredService<IWebHostEnvironment>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var log = services.GetRequiredService<ILoggerFactory>().CreateLogger("VeritabaniHazirlik");

            // 1) Veritabanı ve tablolar yoksa oluştur, eksik migration'ları uygula
            await db.Database.MigrateAsync();

            // 2) Admin rolü
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            // 3) Ortak admin hesabı
            await AdminHesabiniHazirlaAsync(config, userManager, log);

            // 4) Admin'in Google Books'tan çektiği kitaplar (Data/kitaplar.json)
            var eklenen = await KitapDosyasi.YukleAsync(db, env.ContentRootPath);
            if (eklenen > 0)
                log.LogInformation("kitaplar.json'dan {Sayi} kitap eklendi.", eklenen);
        }

        private static async Task AdminHesabiniHazirlaAsync(
            IConfiguration config, UserManager<IdentityUser> userManager, ILogger log)
        {
            var email = config["AdminEmail"];       // appsettings.json (GitHub'da)
            var sifre = config["AdminPassword"];    // user-secrets (sadece bilgisayarında)

            if (string.IsNullOrWhiteSpace(email))
            {
                log.LogWarning("appsettings.json'da AdminEmail yok, admin hesabı oluşturulmadı.");
                return;
            }

            var admin = await userManager.FindByEmailAsync(email);

            if (admin == null)
            {
                if (string.IsNullOrWhiteSpace(sifre))
                {
                    log.LogWarning(
                        "Admin şifresi tanımlı değil, admin hesabı oluşturulmadı. " +
                        "Şunu çalıştır: dotnet user-secrets set \"AdminPassword\" \"sifre\"");
                    return;
                }

                admin = new IdentityUser
                {
                    UserName = "admin",   // girişte mail ya da "admin" yazılabilir
                    Email = email,
                    EmailConfirmed = true
                };

                var sonuc = await userManager.CreateAsync(admin, sifre);
                if (!sonuc.Succeeded)
                {
                    log.LogError("Admin hesabı oluşturulamadı: {Hatalar}",
                        string.Join(", ", sonuc.Errors.Select(e => e.Description)));
                    return;
                }

                log.LogInformation("Admin hesabı oluşturuldu: {Email}", email);
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
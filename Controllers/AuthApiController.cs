using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Controllers.Api
{
    // Flutter uygulamasının giriş / kayıt API'si.
    // Webdeki aynı Identity tablolarını kullanır: webde açılan hesapla
    // mobilde, mobilde açılan hesapla webde giriş yapılabilir.
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AuthApiController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // POST /api/auth/register -> yeni hesap açar (veritabanına yazar)
        [HttpPost("register")]
        public async Task<ActionResult<KullaniciDto>> Register(KayitIstegi istek)
        {
            var user = new IdentityUser
            {
                UserName = istek.UserName.Trim(),
                Email = istek.Email.Trim()
            };

            // Şifre veritabanına düz değil, şifrelenmiş (hash) olarak kaydedilir
            var sonuc = await _userManager.CreateAsync(user, istek.Password);
            if (!sonuc.Succeeded)
            {
                return BadRequest(new { message = HataMesaji(sonuc) });
            }

            return Ok(await DtoYap(user));
        }

        // POST /api/auth/login -> e-posta veya kullanıcı adıyla giriş (webdeki gibi)
        [HttpPost("login")]
        public async Task<ActionResult<KullaniciDto>> Login(GirisIstegi istek)
        {
            var giris = istek.Login.Trim();
            var user = await _userManager.FindByEmailAsync(giris)
                       ?? await _userManager.FindByNameAsync(giris);

            if (user == null || !await _userManager.CheckPasswordAsync(user, istek.Password))
            {
                return Unauthorized(new { message = "Incorrect email/username or password." });
            }

            return Ok(await DtoYap(user));
        }

        private async Task<KullaniciDto> DtoYap(IdentityUser user) =>
            new(user.UserName ?? "", user.Email ?? "",
                await _userManager.IsInRoleAsync(user, "Admin"));

        private static string HataMesaji(IdentityResult sonuc)
        {
            if (sonuc.Errors.Any(e => e.Code == "DuplicateEmail"))
                return "This email is already registered. Please log in.";
            if (sonuc.Errors.Any(e => e.Code == "DuplicateUserName"))
                return "This username is already taken.";
            return string.Join(" ", sonuc.Errors.Select(e => e.Description));
        }
    }

    // Uygulamaya dönen kullanıcı bilgisi (şifre asla geri gönderilmez)
    public record KullaniciDto(string UserName, string Email, bool IsAdmin);

    public class KayitIstegi
    {
        [Required, MaxLength(256)]
        public string UserName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }

    public class GirisIstegi
    {
        [Required]
        public string Login { get; set; } = "";   // e-posta veya kullanıcı adı

        [Required]
        public string Password { get; set; } = "";
    }
}
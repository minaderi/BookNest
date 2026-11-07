using BookNest.Data;
using BookNest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Pages
{
    // Kitaplarım sayfası: kullanıcının raflarındaki ve favorilerindeki kitaplar.
    // Giriş yapmayan kullanıcı otomatik olarak giriş sayfasına yönlendirilir.
    [Authorize]
    public class MyBooksModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MyBooksModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Seçili sekme: boş = tümü, "favori" = favoriler, ya da bir raf kodu
        [BindProperty(SupportsGet = true)]
        public string? Raf { get; set; }

        public List<UserBook> Kayitlar { get; set; } = new();
        public Dictionary<string, int> RafSayilari { get; set; } = new();
        public int ToplamSayi { get; set; }
        public int FavoriSayisi { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            var hepsi = await _context.UserBooks
                .AsNoTracking()
                .Include(u => u.Book)
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            var raftakiler = hepsi.Where(k => Raflar.GecerliMi(k.Status)).ToList();

            ToplamSayi = raftakiler.Count;
            FavoriSayisi = hepsi.Count(k => k.IsFavorite);
            foreach (var raf in Raflar.Hepsi)
                RafSayilari[raf.Kod] = raftakiler.Count(k => k.Status == raf.Kod);

            if (Raf == "favori")
                Kayitlar = hepsi.Where(k => k.IsFavorite).ToList();
            else if (Raflar.GecerliMi(Raf))
                Kayitlar = raftakiler.Where(k => k.Status == Raf).ToList();
            else
            {
                Raf = null;
                Kayitlar = raftakiler;
            }

            return Page();
        }
    }
}
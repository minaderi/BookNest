using BookNest.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Pages.Admin
{
    // Admin paneli: tüm kitapların listesi, arama ve silme
    [Authorize(Roles = "Admin")]
    public class BooksModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BooksModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [TempData]
        public string? Mesaj { get; set; }

        public List<KitapSatiri> Kitaplar { get; set; } = new();
        public int ToplamKitap { get; set; }

        public record KitapSatiri(
            int Id, string Title, string Author, string Category, int PublishYear,
            string CoverImageUrl, int PuanSayisi, int YorumSayisi);

        public async Task OnGetAsync()
        {
            ToplamKitap = await _context.Books.CountAsync();

            var sorgu = _context.Books.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();
                sorgu = sorgu.Where(b => b.Title.Contains(q) || b.Author.Contains(q));
            }

            Kitaplar = await sorgu
                .OrderBy(b => b.Title)
                .Select(b => new KitapSatiri(
                    b.Id,
                    b.Title,
                    b.Author,
                    b.Category.Name,
                    b.PublishYear,
                    b.CoverImageUrl,
                    b.Reviews.Count(),
                    b.Reviews.Count(r => r.Comment != "")))
                .ToListAsync();
        }

        // Kitabı ve ona bağlı her şeyi siler, ortak listeleri günceller
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var kitap = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (kitap == null)
                return RedirectToPage(new { Q });

            var baslik = kitap.Title;
            var yazar = kitap.Author;

            // Puan, yorum, beğeni, yanıt ve raf kayıtları veritabanı tarafından otomatik silinir
            _context.Books.Remove(kitap);
            await _context.SaveChangesAsync();

            // kitaplar.json'u güncelle: kitap bir sonraki açılışta geri gelmesin
            await KitapDosyasi.YazAsync(_context, _env.ContentRootPath);

            // Silinenler listesine ekle: arkadaşların çektiğinde onlarda da silinsin
            await SilinenKitaplar.EkleAsync(baslik, yazar, _env.ContentRootPath);

            Mesaj = $"\"{baslik}\" was deleted.";
            return RedirectToPage(new { Q });
        }
    }
}
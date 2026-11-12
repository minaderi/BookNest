using BookNest.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Pages.Admin
{
    // Admin paneli: yorum ve yanıt moderasyonu
    [Authorize(Roles = "Admin")]
    public class ReviewsModel : PageModel
    {
        private const int EnFazla = 50; // sayfada gösterilecek en fazla yorum

        private readonly AppDbContext _context;

        public ReviewsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [TempData]
        public string? Mesaj { get; set; }

        public int ToplamYorum { get; set; }
        public int ToplamYanit { get; set; }
        public int GosterilenSinir => EnFazla;
        public List<YorumSatiri> Yorumlar { get; set; } = new();

        public record YanitSatiri(int Id, string Kullanici, string Metin, DateTime Tarih);

        public record YorumSatiri(
            int Id, int BookId, string Kitap, string Kullanici, int Puan, string Yorum,
            int BegeniSayisi, List<YanitSatiri> Yanitlar);

        public async Task OnGetAsync()
        {
            ToplamYorum = await _context.Reviews.CountAsync(r => r.Comment != "");
            ToplamYanit = await _context.ReviewReplies.CountAsync();

            var sorgu = _context.Reviews.AsNoTracking().Where(r => r.Comment != "");

            // Arama: yorum metni, yorumu yazan, kitap adı ya da yanıtlarda geçen kelime
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();
                sorgu = sorgu.Where(r =>
                    r.Comment.Contains(q) ||
                    r.User.UserName!.Contains(q) ||
                    r.Book.Title.Contains(q) ||
                    _context.ReviewReplies.Any(y => y.ReviewId == r.Id
                        && (y.Text.Contains(q) || y.User.UserName!.Contains(q))));
            }

            var yorumlar = await sorgu
                .OrderByDescending(r => r.Id)
                .Take(EnFazla)
                .Select(r => new
                {
                    r.Id,
                    r.BookId,
                    Kitap = r.Book.Title,
                    Kullanici = r.User.UserName,
                    r.Rating,
                    r.Comment,
                    Begeni = _context.ReviewLikes.Count(l => l.ReviewId == r.Id)
                })
                .ToListAsync();

            var idler = yorumlar.Select(r => r.Id).ToList();
            var yanitlar = await _context.ReviewReplies
                .AsNoTracking()
                .Where(y => idler.Contains(y.ReviewId))
                .OrderBy(y => y.CreatedAt)
                .Select(y => new { y.Id, y.ReviewId, Kullanici = y.User.UserName, y.Text, y.CreatedAt })
                .ToListAsync();

            Yorumlar = yorumlar
                .Select(r => new YorumSatiri(
                    r.Id, r.BookId, r.Kitap, r.Kullanici ?? "Unknown", r.Rating, r.Comment, r.Begeni,
                    yanitlar
                        .Where(y => y.ReviewId == r.Id)
                        .Select(y => new YanitSatiri(y.Id, y.Kullanici ?? "Unknown", y.Text, y.CreatedAt))
                        .ToList()))
                .ToList();
        }

        // Yorumu kaldırır. Kullanıcının yıldız puanı kalır; yorum metni, beğenileri ve yanıtları silinir.
        public async Task<IActionResult> OnPostRemoveReviewAsync(int reviewId)
        {
            var yorum = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (yorum != null)
            {
                yorum.Comment = "";
                _context.ReviewLikes.RemoveRange(_context.ReviewLikes.Where(l => l.ReviewId == reviewId));
                _context.ReviewReplies.RemoveRange(_context.ReviewReplies.Where(y => y.ReviewId == reviewId));
                await _context.SaveChangesAsync();

                Mesaj = $"Review by {yorum.User.UserName} on \"{yorum.Book.Title}\" was removed.";
            }

            return RedirectToPage(new { Q });
        }

        // Tek bir yanıtı siler
        public async Task<IActionResult> OnPostRemoveReplyAsync(int replyId)
        {
            var yanit = await _context.ReviewReplies
                .Include(y => y.User)
                .FirstOrDefaultAsync(y => y.Id == replyId);

            if (yanit != null)
            {
                _context.ReviewReplies.Remove(yanit);
                await _context.SaveChangesAsync();

                Mesaj = $"Reply by {yanit.User.UserName} was removed.";
            }

            return RedirectToPage(new { Q });
        }
    }
}
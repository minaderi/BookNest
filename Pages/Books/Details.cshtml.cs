using BookNest.Data;
using BookNest.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Pages.Books
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Book? Book { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int? MyRating { get; set; }

        // Giriş yapan kullanıcının bu kitaptaki rafı (yoksa null) ve favori durumu
        public string? MyShelf { get; set; }
        public bool IsFavorite { get; set; }

        // Yorumlar
        public const int YorumMaxUzunluk = 2000;
        public string? MyComment { get; set; }
        public List<YorumSatiri> Yorumlar { get; set; } = new();

        public record YorumSatiri(string KullaniciAdi, int Puan, string Yorum, bool BenimMi);

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!await LoadAsync(id))
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostRateAsync(int id, int rating)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            if (rating < 1 || rating > 5)
                return BadRequest();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            if (!await _context.Books.AnyAsync(b => b.Id == id))
                return NotFound();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookId == id && r.UserId == userId);

            if (review == null)
            {
                _context.Reviews.Add(new Review
                {
                    BookId = id,
                    UserId = userId,
                    Rating = rating,
                    Comment = ""
                });
            }
            else
            {
                review.Rating = rating;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        // Kitabı bir rafa koyar ya da raftan çıkarır (status = "remove")
        public async Task<IActionResult> OnPostShelfAsync(int id, string status)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            if (!await _context.Books.AnyAsync(b => b.Id == id))
                return NotFound();

            var kayit = await _context.UserBooks
                .FirstOrDefaultAsync(u => u.BookId == id && u.UserId == userId);

            if (status == "remove")
            {
                if (kayit != null)
                {
                    if (kayit.IsFavorite)
                    {
                        // Favori kalsın, sadece raftan çıksın
                        kayit.Status = "";
                        kayit.ReadDate = null;
                    }
                    else
                    {
                        _context.UserBooks.Remove(kayit);
                    }
                }
            }
            else if (Raflar.GecerliMi(status))
            {
                if (kayit == null)
                {
                    kayit = new UserBook { UserId = userId, BookId = id, Status = "" };
                    _context.UserBooks.Add(kayit);
                }

                kayit.Status = status;
                kayit.ReadDate = status == Raflar.Okudum
                    ? (kayit.ReadDate ?? DateTime.Now)
                    : null;
            }
            else
            {
                return BadRequest();
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        // Favoriye ekler ya da favoriden çıkarır
        public async Task<IActionResult> OnPostFavoriteAsync(int id)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            if (!await _context.Books.AnyAsync(b => b.Id == id))
                return NotFound();

            var kayit = await _context.UserBooks
                .FirstOrDefaultAsync(u => u.BookId == id && u.UserId == userId);

            if (kayit == null)
            {
                _context.UserBooks.Add(new UserBook
                {
                    UserId = userId,
                    BookId = id,
                    Status = "",
                    IsFavorite = true
                });
            }
            else if (kayit.IsFavorite && string.IsNullOrEmpty(kayit.Status))
            {
                // Rafta değil, sadece favoriydi: kaydı tamamen sil
                _context.UserBooks.Remove(kayit);
            }
            else
            {
                kayit.IsFavorite = !kayit.IsFavorite;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        // Yorumu kaydeder ya da günceller. Yorum yazmak için önce puan verilmiş olmalı.
        public async Task<IActionResult> OnPostReviewAsync(int id, string? comment)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookId == id && r.UserId == userId);

            if (review == null)
                return RedirectToPage(new { id }); // önce puan vermeli

            var metin = (comment ?? "").Trim();
            if (metin.Length > YorumMaxUzunluk)
                metin = metin.Substring(0, YorumMaxUzunluk);

            review.Comment = metin;
            await _context.SaveChangesAsync();
            return RedirectToPage(null, null, new { id }, "reviews");
        }

        // Yorumu siler, puan kalır
        public async Task<IActionResult> OnPostDeleteReviewAsync(int id)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookId == id && r.UserId == userId);

            if (review != null)
            {
                review.Comment = "";
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(null, null, new { id }, "reviews");
        }

        private async Task<bool> LoadAsync(int id)
        {
            Book = await _context.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (Book == null)
                return false;

            var ratings = _context.Reviews.Where(r => r.BookId == id);
            RatingCount = await ratings.CountAsync();
            AverageRating = RatingCount > 0
                ? await ratings.AverageAsync(r => (double)r.Rating)
                : 0;

            var benimId = User.Identity?.IsAuthenticated == true
                ? _userManager.GetUserId(User)
                : null;

            // Yorumu olan puanlar, en yeniden eskiye
            var yorumlar = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.BookId == id && r.Comment != "")
                .OrderByDescending(r => r.Id)
                .Select(r => new { r.UserId, r.User.UserName, r.Rating, r.Comment })
                .ToListAsync();

            Yorumlar = yorumlar
                .Select(r => new YorumSatiri(
                    r.UserName ?? "Unknown", r.Rating, r.Comment, r.UserId == benimId))
                .ToList();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                MyRating = await _context.Reviews
                    .Where(r => r.BookId == id && r.UserId == userId)
                    .Select(r => (int?)r.Rating)
                    .FirstOrDefaultAsync();

                var kayit = await _context.UserBooks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.BookId == id && u.UserId == userId);

                MyShelf = Raflar.GecerliMi(kayit?.Status) ? kayit!.Status : null;
                IsFavorite = kayit?.IsFavorite ?? false;

                MyComment = await _context.Reviews
                    .Where(r => r.BookId == id && r.UserId == userId)
                    .Select(r => r.Comment)
                    .FirstOrDefaultAsync();
            }

            return true;
        }
    }
}
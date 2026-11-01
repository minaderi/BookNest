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

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                MyRating = await _context.Reviews
                    .Where(r => r.BookId == id && r.UserId == userId)
                    .Select(r => (int?)r.Rating)
                    .FirstOrDefaultAsync();
            }

            return true;
        }
    }
}
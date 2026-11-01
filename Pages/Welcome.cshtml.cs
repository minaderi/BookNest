using BookNest.Data;
using BookNest.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Pages
{
    public class WelcomeModel : PageModel
    {
        private readonly AppDbContext _context;

        public WelcomeModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Book> FeaturedBooks { get; set; } = new();
        public int BookCount { get; set; }
        public int RatingCount { get; set; }

        public async Task OnGetAsync()
        {
            BookCount = await _context.Books.CountAsync();
            RatingCount = await _context.Reviews.CountAsync();

            FeaturedBooks = await _context.Books
                .Include(b => b.Category)
                .OrderByDescending(b => b.Id)
                .Take(6)
                .ToListAsync();
        }
    }
}
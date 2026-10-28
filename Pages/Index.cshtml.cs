using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BookNest.Data;
using BookNest.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookNest.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Book> Books { get; set; }

        public async Task OnGetAsync()
        {
            // Kitapları ve bağlı oldukları kategorileri veritabanından çekiyoruz
            Books = await _context.Books
                .Include(b => b.Category)
                .ToListAsync();
        }
    }
}
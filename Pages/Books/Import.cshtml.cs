using BookNest.Data;
using BookNest.Models;
using BookNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Pages.Books
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly GoogleBooksService _googleBooks;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env; // proje klasörünün yolunu bulmak için

               public ImportModel(GoogleBooksService googleBooks, AppDbContext context, IWebHostEnvironment env)
        {
            _googleBooks = googleBooks;
            _context = context;
            _env = env;
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        public List<BookSearchResult> Results { get; set; } = new();

        [TempData]
        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrWhiteSpace(Q))
                Results = await _googleBooks.SearchAsync(Q);
        }

        public async Task<IActionResult> OnPostAddAsync(
            string title, string author, string? description,
            string? coverImageUrl, int publishYear, string? category)
        {
            title = Truncate(title, 200);
            author = Truncate(string.IsNullOrWhiteSpace(author) ? "Bilinmiyor" : author, 150);

            var exists = await _context.Books
                .AnyAsync(b => b.Title == title && b.Author == author);

            if (exists)
            {
                Message = $"\"{title}\" zaten kayıtlı.";
                return RedirectToPage(new { Q });
            }

            var categoryName = Truncate(string.IsNullOrWhiteSpace(category) ? "Diğer" : category, 100);

            var cat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
            if (cat == null)
            {
                cat = new Category { Name = categoryName };
                _context.Categories.Add(cat);
            }

            _context.Books.Add(new Book
            {
                Title = title,
                Author = author,
                Description = description ?? "",
                CoverImageUrl = coverImageUrl ?? "",
                PublishYear = publishYear,
                Category = cat
            });

            await _context.SaveChangesAsync();
            

            // Ekibin geri kalanı da bu kitabı alsın diye kitaplar.json'u güncelle
            await KitapDosyasi.YazAsync(_context, _env.ContentRootPath);

          

            Message = $"\"{title}\" eklendi.";
            return RedirectToPage(new { Q });
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value.Substring(0, max);
    }
}
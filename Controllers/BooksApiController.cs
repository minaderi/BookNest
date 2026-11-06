using System.ComponentModel.DataAnnotations;
using BookNest.Data;
using BookNest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Controllers.Api
{
    // Flutter uygulamasının kullandığı kitap API'si.
    // Web sitesiyle aynı AppDbContext'i, yani aynı MSSQL veritabanını kullanır.
    [ApiController]
    [Route("api/books")]
    public class BooksApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BooksApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/books  -> tüm kitaplar
        // GET /api/books?q=dracula -> başlıkta veya yazarda arama
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAll([FromQuery] string? q)
        {
            var query = _context.Books.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var aranan = q.Trim();
                query = query.Where(b => b.Title.Contains(aranan) || b.Author.Contains(aranan));
            }

            var books = await query
                .OrderBy(b => b.Category.Name)
                .ThenBy(b => b.Title)
                .Select(b => new BookDto(
                    b.Id, b.Title, b.Author, b.Description, b.CoverImageUrl,
                    b.PublishYear, b.Category.Name))
                .ToListAsync();

            return Ok(books);
        }

        // GET /api/books/5 -> tek kitap
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BookDto>> Get(int id)
        {
            var book = await _context.Books
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(b => new BookDto(
                    b.Id, b.Title, b.Author, b.Description, b.CoverImageUrl,
                    b.PublishYear, b.Category.Name))
                .FirstOrDefaultAsync();

            return book is null ? NotFound() : Ok(book);
        }

        // GET /api/books/categories -> kategori adları (uygulamadaki ekleme formu için)
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var names = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();

            return Ok(names);
        }

        // POST /api/books -> yeni kitap ekler (Books/Import sayfasıyla aynı mantık)
        [HttpPost]
        public async Task<ActionResult<BookDto>> Create(CreateBookRequest request)
        {
            var title = Truncate(request.Title.Trim(), 200);
            var author = Truncate(request.Author.Trim(), 150);

            // Aynı kitap varsa tekrar ekleme
            var exists = await _context.Books
                .AnyAsync(b => b.Title == title && b.Author == author);
            if (exists)
                return Conflict(new { message = $"\"{title}\" zaten kayıtlı." });

            // Kategori yoksa oluştur
            var categoryName = Truncate(
                string.IsNullOrWhiteSpace(request.Category) ? "Diğer" : request.Category.Trim(), 100);

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
            if (category == null)
            {
                category = new Category { Name = categoryName };
                _context.Categories.Add(category);
            }

            var book = new Book
            {
                Title = title,
                Author = author,
                Description = request.Description?.Trim() ?? "",
                CoverImageUrl = request.CoverImageUrl?.Trim() ?? "",
                PublishYear = request.PublishYear ?? 0,
                Category = category
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var dto = new BookDto(book.Id, book.Title, book.Author, book.Description,
                book.CoverImageUrl, book.PublishYear, category.Name);

            return CreatedAtAction(nameof(Get), new { id = book.Id }, dto);
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value.Substring(0, max);
    }

    // API'nin döndürdüğü kitap bilgisi
    public record BookDto(
        int Id,
        string Title,
        string Author,
        string Description,
        string CoverImageUrl,
        int PublishYear,
        string Category);

    // Uygulamanın kitap eklerken gönderdiği bilgi
    public class CreateBookRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [Required, MaxLength(150)]
        public string Author { get; set; } = "";

        public string? Description { get; set; }
        public string? CoverImageUrl { get; set; }

        [Range(0, 3000)]
        public int? PublishYear { get; set; }

        public string? Category { get; set; }
    }
}
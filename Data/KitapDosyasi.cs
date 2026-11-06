using System.Text.Encodings.Web;
using System.Text.Json;
using BookNest.Models;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Data
{
    // Kitapları veritabanı ile Data/kitaplar.json dosyası arasında taşır.
    // kitaplar.json GitHub'a gider; böylece admin'in Google Books'tan çektiği
    // kitaplar, projeyi çeken herkesin veritabanına da eklenir.
    public static class KitapDosyasi
    {
        // JSON dosyasında her kitap için tutulan bilgiler
        public record KitapKaydi(
            string Title,
            string Author,
            string Description,
            string CoverImageUrl,
            int PublishYear,
            string Category);

        private static readonly JsonSerializerOptions JsonAyarlari = new()
        {
            WriteIndented = true,                                  // okunaklı, satır satır yaz
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // Türkçe karakterler bozulmasın
        };

        private static string DosyaYolu(string projeKlasoru)
            => Path.Combine(projeKlasoru, "Data", "kitaplar.json");

        // Veritabanındaki tüm kitapları kitaplar.json'a yazar.
        public static async Task YazAsync(AppDbContext context, string projeKlasoru)
        {
            var kitaplar = await context.Books
                .AsNoTracking()
                .OrderBy(b => b.Title)
                .ThenBy(b => b.Author)
                .Select(b => new KitapKaydi(
                    b.Title, b.Author, b.Description, b.CoverImageUrl,
                    b.PublishYear, b.Category.Name))
                .ToListAsync();

            var json = JsonSerializer.Serialize(kitaplar, JsonAyarlari);
            await File.WriteAllTextAsync(DosyaYolu(projeKlasoru), json);
        }

        // kitaplar.json'daki kitaplardan veritabanında olmayanları ekler.
        // Kaç kitap eklendiğini döndürür.
        public static async Task<int> YukleAsync(AppDbContext context, string projeKlasoru)
        {
            var yol = DosyaYolu(projeKlasoru);
            if (!File.Exists(yol))
                return 0; // henüz hiç kitap çekilmemiş

            var json = await File.ReadAllTextAsync(yol);
            var kayitlar = JsonSerializer.Deserialize<List<KitapKaydi>>(json, JsonAyarlari) ?? new();

            var eklenen = 0;
            foreach (var k in kayitlar)
            {
                if (string.IsNullOrWhiteSpace(k.Title) || string.IsNullOrWhiteSpace(k.Author))
                    continue;

                // Aynı başlık + yazar zaten varsa atla
                var varMi = await context.Books.AnyAsync(b => b.Title == k.Title && b.Author == k.Author);
                if (varMi)
                    continue;

                // Kategoriyi bul, yoksa oluştur
                var kategoriAdi = string.IsNullOrWhiteSpace(k.Category) ? "Diğer" : k.Category;
                var kategori = context.Categories.Local.FirstOrDefault(c => c.Name == kategoriAdi)
                    ?? await context.Categories.FirstOrDefaultAsync(c => c.Name == kategoriAdi);
                if (kategori == null)
                {
                    kategori = new Category { Name = kategoriAdi };
                    context.Categories.Add(kategori);
                }

                context.Books.Add(new Book
                {
                    Title = k.Title,
                    Author = k.Author,
                    Description = k.Description ?? "",
                    CoverImageUrl = k.CoverImageUrl ?? "",
                    PublishYear = k.PublishYear,
                    Category = kategori
                });
                eklenen++;
            }

            if (eklenen > 0)
                await context.SaveChangesAsync();

            return eklenen;
        }
    }
}
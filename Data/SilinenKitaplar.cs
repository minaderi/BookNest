using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Data
{
    // Admin'in sildiği kitapların listesi (Data/silinen-kitaplar.json).
    // Bu dosya GitHub'a gider. Projeyi çeken herkesin bilgisayarında, program açılırken
    // bu listedeki kitaplar veritabanından da silinir. Böylece silme işlemi tüm ekibe yayılır.
    //
    // Neden kitaplar.json'da olmayanları silmiyoruz? Çünkü bir arkadaş yeni bir kitap ekleyip
    // henüz GitHub'a göndermediyse, onun kitabı yanlışlıkla silinebilirdi. Bu listede sadece
    // bilerek silinen kitaplar olduğu için başkasının kitabı asla kaybolmaz.
    public static class SilinenKitaplar
    {
        public record SilinenKayit(string Title, string Author);

        private static readonly JsonSerializerOptions JsonAyarlari = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static string DosyaYolu(string projeKlasoru)
            => Path.Combine(projeKlasoru, "Data", "silinen-kitaplar.json");

        private static async Task<List<SilinenKayit>> OkuAsync(string projeKlasoru)
        {
            var yol = DosyaYolu(projeKlasoru);
            if (!File.Exists(yol))
                return new();

            var json = await File.ReadAllTextAsync(yol);
            return JsonSerializer.Deserialize<List<SilinenKayit>>(json, JsonAyarlari) ?? new();
        }

        private static async Task YazAsync(List<SilinenKayit> liste, string projeKlasoru)
        {
            var sirali = liste.OrderBy(k => k.Title).ThenBy(k => k.Author).ToList();
            var json = JsonSerializer.Serialize(sirali, JsonAyarlari);
            await File.WriteAllTextAsync(DosyaYolu(projeKlasoru), json);
        }

        // Silinen bir kitabı listeye ekler
        public static async Task EkleAsync(string title, string author, string projeKlasoru)
        {
            var liste = await OkuAsync(projeKlasoru);
            if (liste.Any(k => k.Title == title && k.Author == author))
                return;

            liste.Add(new SilinenKayit(title, author));
            await YazAsync(liste, projeKlasoru);
        }

        // Silinmiş bir kitap tekrar eklenirse listeden çıkarır
        public static async Task CikarAsync(string title, string author, string projeKlasoru)
        {
            var liste = await OkuAsync(projeKlasoru);
            var kalan = liste.Where(k => !(k.Title == title && k.Author == author)).ToList();
            if (kalan.Count != liste.Count)
                await YazAsync(kalan, projeKlasoru);
        }

        // Listedeki kitaplar veritabanında hâlâ varsa siler. Kaç kitap silindiğini döndürür.
        // Kitaba bağlı puan, yorum, beğeni, yanıt ve raf kayıtları veritabanı tarafından
        // otomatik silinir (cascade).
        public static async Task<int> UygulaAsync(AppDbContext context, string projeKlasoru)
        {
            var liste = await OkuAsync(projeKlasoru);
            var silinen = 0;

            foreach (var k in liste)
            {
                var kitaplar = await context.Books
                    .Where(b => b.Title == k.Title && b.Author == k.Author)
                    .ToListAsync();

                context.Books.RemoveRange(kitaplar);
                silinen += kitaplar.Count;
            }

            if (silinen > 0)
                await context.SaveChangesAsync();

            return silinen;
        }
    }
}
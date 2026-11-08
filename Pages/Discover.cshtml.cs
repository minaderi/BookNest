using BookNest.Data;
using BookNest.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Pages
{
    // Keşfet sayfası: kitap adına / yazara göre arama; kategori, dönem, puan ve raf filtreleri;
    // sıralama ve sayfalama. Filtreler adres çubuğunda durur, böylece bağlantı paylaşılabilir.
    public class DiscoverModel : PageModel
    {
        private const int SayfaBoyutu = 24;

        // SQL Server karşılaştırma kuralı: büyük/küçük harf (CI) ve aksan (AI) farkını yok sayar
        private const string AramaKarsilastirma = "Latin1_General_CI_AI";

        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DiscoverModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Sıralama seçenekleri: kod ve ekranda görünen ad
        public static readonly (string Kod, string Ad)[] Siralamalar =
        {
            ("title", "Title A–Z"),
            ("newest", "Newest"),
            ("rating", "Highest rated"),
            ("popular", "Most popular")
        };

        // Yayın dönemleri: kod, ad, başlangıç ve bitiş yılı (dahil)
        public static readonly (string Kod, string Ad, int Bas, int Bit)[] Donemler =
        {
            ("2020s", "2020s", 2020, 9999),
            ("2010s", "2010s", 2010, 2019),
            ("2000s", "2000s", 2000, 2009),
            ("1900s", "20th century", 1900, 1999),
            ("classic", "Before 1900", 1, 1899)
        };

        // Puan filtreleri: kod ve en düşük ortalama puan
        public static readonly (string Kod, string Ad, double EnAz)[] Puanlar =
        {
            ("4", "4+", 4),
            ("3", "3+", 3)
        };

        // Raf filtreleri (sadece giriş yapanlar için)
        public static readonly (string Kod, string Ad)[] RafFiltreleri =
        {
            ("on", "On my shelves"),
            ("off", "Not added yet")
        };

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Era { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Rating { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Shelf { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sort { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Sayfa { get; set; } = 1;

        public List<KitapSatiri> Kitaplar { get; set; } = new();
        public List<(string Ad, int Sayi)> Kategoriler { get; set; } = new();
        public Dictionary<string, int> DonemSayilari { get; set; } = new();
        public Dictionary<string, int> PuanSayilari { get; set; } = new();
        public Dictionary<string, int> RafSayilari { get; set; } = new();
        public int ToplamSonuc { get; set; }
        public int ToplamSayfa { get; set; }
        public bool GirisYapti { get; set; }

        public bool FiltreVar => Q != null || Category != null || Era != null || Rating != null || Shelf != null;

        public class KitapSatiri
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Author { get; set; } = "";
            public string? CoverImageUrl { get; set; }
            public int PublishYear { get; set; }
            public string? CategoryName { get; set; }
            public double AverageRating { get; set; }
            public int RatingCount { get; set; }
            public int ShelfCount { get; set; }
            public string? MyShelf { get; set; }
        }

        // Filtre grupları: sayıları hesaplarken her grup kendi filtresi hariç diğerlerini uygular
        private enum Grup { Hicbiri, Kategori, Donem, Puan, Raf }

        private string? _userId;

        public async Task OnGetAsync()
        {
            Temizle();

            _userId = _userManager.GetUserId(User);
            GirisYapti = _userId != null;
            if (!GirisYapti) Shelf = null;

            // Filtre sayıları (ör. "Fiction 5"): diğer seçili filtrelere göre hesaplanır
            Kategoriler = (await Filtrele(Grup.Kategori)
                    .GroupBy(b => b.Category.Name)
                    .Select(g => new { Ad = g.Key, Sayi = g.Count() })
                    .OrderBy(k => k.Ad)
                    .ToListAsync())
                .Select(k => (k.Ad, k.Sayi))
                .ToList();

            // Seçili kategori diğer filtreler yüzünden boş kalsa bile listede görünsün
            if (Category != null && !Kategoriler.Any(k => k.Ad == Category))
                Kategoriler.Add((Category, 0));

            var yillar = await Filtrele(Grup.Donem).Select(b => b.PublishYear).ToListAsync();
            foreach (var d in Donemler)
                DonemSayilari[d.Kod] = yillar.Count(y => y >= d.Bas && y <= d.Bit);

            var ortalamalar = await Filtrele(Grup.Puan)
                .Select(b => b.Reviews.Average(r => (double?)r.Rating))
                .ToListAsync();
            foreach (var p in Puanlar)
                PuanSayilari[p.Kod] = ortalamalar.Count(o => o >= p.EnAz);

            if (GirisYapti)
            {
                var raftaMi = await Filtrele(Grup.Raf)
                    .Select(b => b.UserBooks.Any(u => u.UserId == _userId && u.Status != ""))
                    .ToListAsync();
                RafSayilari["on"] = raftaMi.Count(x => x);
                RafSayilari["off"] = raftaMi.Count(x => !x);
            }

            // Sonuçlar
            var sorgu = Filtrele(Grup.Hicbiri);
            var satirlar = sorgu.Select(b => new KitapSatiri
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                CoverImageUrl = b.CoverImageUrl,
                PublishYear = b.PublishYear,
                CategoryName = b.Category.Name,
                RatingCount = b.Reviews.Count(),
                AverageRating = b.Reviews.Average(r => (double?)r.Rating) ?? 0,
                ShelfCount = b.UserBooks.Count(u => u.Status != ""),
                MyShelf = b.UserBooks
                    .Where(u => u.UserId == _userId && u.Status != "")
                    .Select(u => u.Status)
                    .FirstOrDefault()
            });

            satirlar = Sort switch
            {
                "newest" => satirlar.OrderByDescending(k => k.PublishYear).ThenBy(k => k.Title),
                "rating" => satirlar.OrderByDescending(k => k.AverageRating).ThenByDescending(k => k.RatingCount).ThenBy(k => k.Title),
                "popular" => satirlar.OrderByDescending(k => k.ShelfCount).ThenBy(k => k.Title),
                _ => satirlar.OrderBy(k => k.Title)
            };

            ToplamSonuc = await sorgu.CountAsync();
            ToplamSayfa = Math.Max(1, (int)Math.Ceiling(ToplamSonuc / (double)SayfaBoyutu));
            Sayfa = Math.Clamp(Sayfa, 1, ToplamSayfa);

            Kitaplar = await satirlar
                .Skip((Sayfa - 1) * SayfaBoyutu)
                .Take(SayfaBoyutu)
                .ToListAsync();
        }

        // Adresten gelen değerleri temizler; tanınmayan değerler yok sayılır
        private void Temizle()
        {
            Q = Q?.Trim();
            if (Q?.Length > 100) Q = Q.Substring(0, 100);
            if (string.IsNullOrEmpty(Q)) Q = null;
            if (string.IsNullOrWhiteSpace(Category)) Category = null;
            if (!Donemler.Any(d => d.Kod == Era)) Era = null;
            if (!Puanlar.Any(p => p.Kod == Rating)) Rating = null;
            if (!RafFiltreleri.Any(r => r.Kod == Shelf)) Shelf = null;
            if (!Siralamalar.Any(s => s.Kod == Sort)) Sort = "title";
        }

        // Arama ve seçili filtreleri uygular; "haric" grubunun filtresi atlanır
        private IQueryable<Book> Filtrele(Grup haric)
        {
            var sorgu = _context.Books.AsNoTracking();

            // Aksan farkı önemsenmez: "oguz" yazınca "Oğuz", "suc" yazınca "Suç" da bulunur
            if (Q != null)
                sorgu = sorgu.Where(b =>
                    EF.Functions.Collate(b.Title, AramaKarsilastirma).Contains(Q) ||
                    EF.Functions.Collate(b.Author, AramaKarsilastirma).Contains(Q));

            if (haric != Grup.Kategori && Category != null)
                sorgu = sorgu.Where(b => b.Category.Name == Category);

            if (haric != Grup.Donem && Era != null)
            {
                var d = Donemler.First(x => x.Kod == Era);
                sorgu = sorgu.Where(b => b.PublishYear >= d.Bas && b.PublishYear <= d.Bit);
            }

            if (haric != Grup.Puan && Rating != null)
            {
                var enAz = Puanlar.First(x => x.Kod == Rating).EnAz;
                sorgu = sorgu.Where(b => b.Reviews.Average(r => (double?)r.Rating) >= enAz);
            }

            if (haric != Grup.Raf && Shelf != null)
            {
                sorgu = Shelf == "on"
                    ? sorgu.Where(b => b.UserBooks.Any(u => u.UserId == _userId && u.Status != ""))
                    : sorgu.Where(b => !b.UserBooks.Any(u => u.UserId == _userId && u.Status != ""));
            }

            return sorgu;
        }

        // Mevcut filtreleri koruyarak yeni bir adres üretir.
        // Örnek: Rota(("era", "2020s")) dönem filtresini değiştirir, Rota(("era", null)) kaldırır.
        // Sayfa numarası ayrıca verilmezse 1'e döner.
        public Dictionary<string, string> Rota(params (string Anahtar, string? Deger)[] degisiklikler)
        {
            var rota = new Dictionary<string, string?>
            {
                ["q"] = Q,
                ["category"] = Category,
                ["era"] = Era,
                ["rating"] = Rating,
                ["shelf"] = Shelf,
                ["sort"] = Sort == "title" ? null : Sort, // varsayılan sıralama adrese yazılmaz
                ["p"] = null
            };

            foreach (var (anahtar, deger) in degisiklikler)
                rota[anahtar] = anahtar == "sort" && deger == "title" ? null : deger;

            if (rota["p"] == "1") rota["p"] = null;

            return rota.Where(x => !string.IsNullOrEmpty(x.Value)).ToDictionary(x => x.Key, x => x.Value!);
        }
    }
}

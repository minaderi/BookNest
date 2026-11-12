using BookNest.Data;
using BookNest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Pages
{
    // Kitap önerileri ("For You") sayfası.
    // İki bölüm var:
    //   1) Sevdiğin yazar ve kategorilerden kitaplar
    //   2) Senin gibi okuyan kullanıcıların sevdiği kitaplar
    // Yeterli öneri çıkmazsa sitenin en yüksek puanlı kitapları gösterilir.
    [Authorize]
    public class ForYouModel : PageModel
    {
        private const int BolumBasinaKitap = 8;
        private const int SevdimPuani = 4; // 4 ve 5 yıldız "sevdim" sayılır

        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ForYouModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Ekranda gösterilen bir öneri: kitap + neden önerildiği
        public class Oneri
        {
            public Book Kitap { get; set; } = null!;
            public string Neden { get; set; } = "";
            public double Ortalama { get; set; }
            public int PuanSayisi { get; set; }
            public double Skor { get; set; }
        }

        public List<Oneri> YazarKategori { get; set; } = new();
        public List<Oneri> BenzerOkuyucular { get; set; } = new();
        public List<Oneri> Populer { get; set; } = new();

        // Kullanıcının sevdiği kitap sayısı (0 ise sayfada yönlendirme mesajı çıkar)
        public int SevilenSayisi { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            // --- Veriyi bir kez çek (proje küçük olduğu için hepsini belleğe almak sorun değil)
            var kitaplar = await _context.Books.AsNoTracking()
                .Include(b => b.Category)
                .ToDictionaryAsync(b => b.Id);

            var puanlar = await _context.Reviews.AsNoTracking()
                .Select(r => new { r.UserId, r.BookId, r.Rating })
                .ToListAsync();

            var raflar = await _context.UserBooks.AsNoTracking()
                .Select(u => new { u.UserId, u.BookId, u.Status, u.IsFavorite })
                .ToListAsync();

            // Kitap başına ortalama puan
            var ortalamalar = puanlar
                .GroupBy(p => p.BookId)
                .ToDictionary(g => g.Key, g => (Ort: g.Average(p => p.Rating), Sayi: g.Count()));

            // --- Benim kitaplarım
            var benimPuanlarim = puanlar.Where(p => p.UserId == userId).ToDictionary(p => p.BookId, p => p.Rating);
            var benimRaflarim = raflar.Where(r => r.UserId == userId).ToList();

            // Daha önce rafa eklediğim ya da puanladığım hiçbir kitap önerilmez
            var haricTut = new HashSet<int>(benimPuanlarim.Keys.Concat(benimRaflarim.Select(r => r.BookId)));

            // Sevdiğim kitaplar: 4-5 yıldız verdiklerim + favorilerim
            var sevdiklerim = new HashSet<int>(
                benimPuanlarim.Where(p => p.Value >= SevdimPuani).Select(p => p.Key)
                    .Concat(benimRaflarim.Where(r => r.IsFavorite).Select(r => r.BookId)));

            // Hiç puan/favori yoksa "Okudum" rafındakileri kullan (düşük puan verdiklerim hariç)
            if (sevdiklerim.Count == 0)
            {
                foreach (var r in benimRaflarim.Where(r => r.Status == Raflar.Okudum))
                    if (!benimPuanlarim.TryGetValue(r.BookId, out var p) || p > 2)
                        sevdiklerim.Add(r.BookId);
            }

            sevdiklerim.RemoveWhere(id => !kitaplar.ContainsKey(id));
            SevilenSayisi = sevdiklerim.Count;

            Oneri OneriYap(Book b, string neden, double skor)
            {
                var o = ortalamalar.TryGetValue(b.Id, out var x) ? x : (Ort: 0d, Sayi: 0);
                return new Oneri { Kitap = b, Neden = neden, Ortalama = o.Ort, PuanSayisi = o.Sayi, Skor = skor };
            }

            // =============== 1) Sevdiğin yazar ve kategorilerden ===============
            var sevilenKitaplar = sevdiklerim.Select(id => kitaplar[id]).ToList();

            foreach (var aday in kitaplar.Values.Where(b => !haricTut.Contains(b.Id)))
            {
                var ayniYazar = sevilenKitaplar.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.Author) &&
                    string.Equals(s.Author.Trim(), aday.Author?.Trim(), StringComparison.OrdinalIgnoreCase));

                var ayniKategori = sevilenKitaplar.Where(s => s.CategoryId == aday.CategoryId).ToList();

                if (ayniYazar == null && ayniKategori.Count == 0)
                    continue;

                var ort = ortalamalar.TryGetValue(aday.Id, out var x) ? x.Ort : 0;

                // Aynı yazar en güçlü sinyal; sonra kategorideki sevilen kitap sayısı; eşitlikte ortalama puan
                var skor = (ayniYazar != null ? 10 : 0) + Math.Min(ayniKategori.Count, 3) * 2 + ort;

                var neden = ayniYazar != null
                    ? $"More from {aday.Author}, because you liked {ayniYazar.Title}"
                    : $"{aday.Category?.Name ?? "Same genre"}, like {ayniKategori[0].Title}";

                YazarKategori.Add(OneriYap(aday, neden, skor));
            }

            YazarKategori = YazarKategori
                .OrderByDescending(o => o.Skor)
                .ThenByDescending(o => o.PuanSayisi)
                .Take(BolumBasinaKitap)
                .ToList();

            // =============== 2) Senin gibi okuyanlar ===============
            if (sevdiklerim.Count > 0)
            {
                // Diğer kullanıcıların sevdiği kitaplar (4-5 yıldız ya da favori)
                var digerSevenler = puanlar
                    .Where(p => p.UserId != userId && p.Rating >= SevdimPuani)
                    .Select(p => (p.UserId, p.BookId))
                    .Concat(raflar.Where(r => r.UserId != userId && r.IsFavorite).Select(r => (r.UserId, r.BookId)))
                    .Distinct()
                    .ToList();

                // Benzer okuyucu: sevdiğim kitaplardan en az birini o da sevmiş.
                // Değeri: ortak sevdiğimiz kitaplar
                var benzerler = digerSevenler
                    .Where(x => sevdiklerim.Contains(x.BookId))
                    .GroupBy(x => x.UserId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.BookId).ToList());

                var ilkBolumdekiler = YazarKategori.Select(o => o.Kitap.Id).ToHashSet();

                var adaylar = digerSevenler
                    .Where(x => benzerler.ContainsKey(x.UserId)
                                && !haricTut.Contains(x.BookId)
                                && !ilkBolumdekiler.Contains(x.BookId)
                                && kitaplar.ContainsKey(x.BookId))
                    .GroupBy(x => x.BookId);

                foreach (var g in adaylar)
                {
                    var okuyucular = g.Select(x => x.UserId).Distinct().ToList();

                    // Ortak kitap sayısı fazla olan okuyucu daha çok ağırlık taşır
                    var skor = okuyucular.Sum(u => benzerler[u].Count);

                    // "Neden" için en çok paylaşılan ortak kitabı bul
                    var ortakKitapId = okuyucular
                        .SelectMany(u => benzerler[u])
                        .GroupBy(id => id)
                        .OrderByDescending(x => x.Count())
                        .First().Key;

                    var neden = okuyucular.Count == 1
                        ? $"A reader who liked {kitaplar[ortakKitapId].Title} loved this"
                        : $"{okuyucular.Count} readers who liked {kitaplar[ortakKitapId].Title} loved this";

                    BenzerOkuyucular.Add(OneriYap(kitaplar[g.Key], neden, skor));
                }

                BenzerOkuyucular = BenzerOkuyucular
                    .OrderByDescending(o => o.Skor)
                    .ThenByDescending(o => o.Ortalama)
                    .Take(BolumBasinaKitap)
                    .ToList();
            }

            // =============== 3) Yedek: en yüksek puanlılar ===============
            if (YazarKategori.Count + BenzerOkuyucular.Count < BolumBasinaKitap)
            {
                var gosterilenler = YazarKategori.Concat(BenzerOkuyucular).Select(o => o.Kitap.Id).ToHashSet();

                Populer = kitaplar.Values
                    .Where(b => !haricTut.Contains(b.Id) && !gosterilenler.Contains(b.Id))
                    .Select(b => OneriYap(b, "", 0))
                    .OrderByDescending(o => o.PuanSayisi > 0)
                    .ThenByDescending(o => o.Ortalama)
                    .ThenByDescending(o => o.PuanSayisi)
                    .Take(BolumBasinaKitap)
                    .ToList();

                foreach (var o in Populer)
                    o.Neden = o.PuanSayisi > 0
                        ? $"Rated {o.Ortalama.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} by {o.PuanSayisi} {(o.PuanSayisi == 1 ? "reader" : "readers")}"
                        : "New on BookNest";
            }

            return Page();
        }
    }
}

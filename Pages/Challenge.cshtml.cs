using System.ComponentModel.DataAnnotations;
using BookNest.Data;
using BookNest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Pages
{
    // Okuma hedefi sayfası: kullanıcı bu yıl kaç kitap okumak istediğini yazar,
    // "Read" rafına koyduğu kitaplara göre ilerlemesini görür.
    [Authorize]
    public class ChallengeModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ChallengeModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int Yil { get; } = DateTime.Now.Year;

        // Kayıtlı hedef (yoksa null)
        public int? Hedef { get; set; }

        // Formdan gelen yeni hedef
        [BindProperty]
        [Range(1, 1000, ErrorMessage = "Please enter a number between 1 and 1000.")]
        public int YeniHedef { get; set; }

        public List<UserBook> OkunanKitaplar { get; set; } = new();
        public int OkunanSayi => OkunanKitaplar.Count;

        // 0-100 arası yüzde (hedef aşılsa bile çubuk 100'de durur)
        public int Yuzde => Hedef is > 0 ? Math.Min(100, OkunanSayi * 100 / Hedef.Value) : 0;

        // Yılın bu noktasına kadar okunmuş olması gereken kitap sayısı
        public int BeklenenSayi
        {
            get
            {
                if (Hedef is not > 0) return 0;
                var gunSayisi = DateTime.IsLeapYear(Yil) ? 366 : 365;
                return (int)Math.Floor(Hedef.Value * (double)DateTime.Now.DayOfYear / gunSayisi);
            }
        }

        // Hedefe ulaşmak için kalan kitap sayısı
        public int KalanKitap => Hedef is > 0 ? Math.Max(0, Hedef.Value - OkunanSayi) : 0;

        // Yılın bitmesine kalan gün (bugün dahil)
        public int KalanGun => (new DateTime(Yil, 12, 31) - DateTime.Today).Days + 1;

        // Hedefe yetişmek için ayda kaç kitap okunmalı (yukarı yuvarlanır)
        public int AylikGereken
        {
            get
            {
                if (KalanKitap == 0) return 0;
                var kalanAy = Math.Max(1.0, KalanGun / 30.44);
                return (int)Math.Ceiling(KalanKitap / kalanAy);
            }
        }

        // Ocak..Aralık her ayda okunan kitap sayısı (grafik için)
        public int[] AylikSayilar { get; set; } = new int[12];

        public int BuAy { get; } = DateTime.Now.Month;

        [TempData]
        public string? Mesaj { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            await YukleAsync(userId);
            // Formda kayıtlı hedef görünsün; hedef yoksa örnek olarak 12 (ayda bir kitap)
            YeniHedef = Hedef ?? 12;

            return Page();
        }

        // Hedefi kaydeder ya da günceller
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            if (!ModelState.IsValid)
            {
                await YukleAsync(userId);
                return Page();
            }

            var kayit = await _context.ReadingGoals
                .FirstOrDefaultAsync(g => g.UserId == userId && g.Year == Yil);

            if (kayit == null)
            {
                _context.ReadingGoals.Add(new ReadingGoal { UserId = userId, Year = Yil, Target = YeniHedef });
                Mesaj = "Your reading goal has been set. Good luck!";
            }
            else
            {
                kayit.Target = YeniHedef;
                Mesaj = "Your reading goal has been updated.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // Hedefi siler
        public async Task<IActionResult> OnPostRemoveAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Challenge();

            var kayit = await _context.ReadingGoals
                .FirstOrDefaultAsync(g => g.UserId == userId && g.Year == Yil);

            if (kayit != null)
            {
                _context.ReadingGoals.Remove(kayit);
                await _context.SaveChangesAsync();
                Mesaj = "Your reading goal has been removed.";
            }

            return RedirectToPage();
        }

        private async Task YukleAsync(string userId)
        {
            Hedef = await _context.ReadingGoals
                .Where(g => g.UserId == userId && g.Year == Yil)
                .Select(g => (int?)g.Target)
                .FirstOrDefaultAsync();

            // Bu yıl "Read" rafına konan kitaplar, en son okunan en başta
            OkunanKitaplar = await _context.UserBooks
                .AsNoTracking()
                .Include(u => u.Book)
                .Where(u => u.UserId == userId
                         && u.Status == Raflar.Okudum
                         && u.ReadDate != null
                         && u.ReadDate.Value.Year == Yil)
                .OrderByDescending(u => u.ReadDate)
                .ToListAsync();

            AylikSayilar = new int[12];
            foreach (var k in OkunanKitaplar)
                AylikSayilar[k.ReadDate!.Value.Month - 1]++;
        }
    }
}

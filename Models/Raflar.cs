namespace BookNest.Models
{
    // Okuma rafları. Veritabanında UserBook.Status alanına bu kodlar yazılır.
    public static class Raflar
    {
        public const string OkumakIstiyorum = "WantToRead";
        public const string Okuyorum = "CurrentlyReading";
        public const string Okudum = "Read";

        // Sayfalarda gösterilecek sıra, ad ve simge (simge şimdilik boş)
        public static readonly (string Kod, string Ad, string Ikon)[] Hepsi =
        {
            (OkumakIstiyorum, "Want to Read", ""),
            (Okuyorum, "Currently Reading", ""),
            (Okudum, "Read", "")
        };

        public static bool GecerliMi(string? kod)
            => Hepsi.Any(r => r.Kod == kod);

        public static string AdiNe(string? kod)
            => Hepsi.Where(r => r.Kod == kod).Select(r => r.Ad).FirstOrDefault() ?? "";

        public static string IkonuNe(string? kod)
            => Hepsi.Where(r => r.Kod == kod).Select(r => r.Ikon).FirstOrDefault() ?? "";
    }
}
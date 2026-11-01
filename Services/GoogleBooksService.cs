using System.Text.Json;

namespace BookNest.Services
{
    public class BookSearchResult
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Description { get; set; } = "";
        public string CoverImageUrl { get; set; } = "";
        public int PublishYear { get; set; }
        public string Category { get; set; } = "";
    }

    public class GoogleBooksService
    {
        private readonly HttpClient _http;
        private readonly string? _apiKey;

        public GoogleBooksService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["GoogleBooks:ApiKey"];
        }

        public async Task<List<BookSearchResult>> SearchAsync(string query)
        {
            var results = new List<BookSearchResult>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            var url = "https://www.googleapis.com/books/v1/volumes"
                    + $"?q={Uri.EscapeDataString(query)}&maxResults=10&printType=books";
                    if (!string.IsNullOrEmpty(_apiKey))
                        url += $"&key={Uri.EscapeDataString(_apiKey)}";

            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return results;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (!doc.RootElement.TryGetProperty("items", out var items)) return results;

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("volumeInfo", out var info)) continue;

                var title = info.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                if (title == "") continue;

                var author = "";
                if (info.TryGetProperty("authors", out var a) && a.ValueKind == JsonValueKind.Array)
                    author = string.Join(", ", a.EnumerateArray().Select(x => x.GetString()));

                var description = info.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

                var year = 0;
                if (info.TryGetProperty("publishedDate", out var p))
                {
                    var date = p.GetString() ?? "";
                    if (date.Length >= 4) int.TryParse(date.Substring(0, 4), out year);
                }

                var category = "";
                if (info.TryGetProperty("categories", out var c) && c.ValueKind == JsonValueKind.Array && c.GetArrayLength() > 0)
                    category = c[0].GetString() ?? "";

                var cover = "";
                if (info.TryGetProperty("imageLinks", out var img) && img.TryGetProperty("thumbnail", out var th))
                    cover = (th.GetString() ?? "").Replace("http://", "https://");

                results.Add(new BookSearchResult
                {
                    Title = title,
                    Author = author,
                    Description = description,
                    CoverImageUrl = cover,
                    PublishYear = year,
                    Category = category
                });
            }

            return results;
        }
    }
}
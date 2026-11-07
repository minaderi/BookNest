using Microsoft.AspNetCore.Identity;

namespace BookNest.Models
{
    // Bir yoruma yazılan yanıt
    public class ReviewReply
    {
        public int Id { get; set; }

        public int ReviewId { get; set; }
        public Review Review { get; set; } = null!;

        public string UserId { get; set; } = "";
        public IdentityUser User { get; set; } = null!;

        public string Text { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
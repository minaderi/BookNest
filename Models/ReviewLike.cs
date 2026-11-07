using Microsoft.AspNetCore.Identity;

namespace BookNest.Models
{
    // Bir kullanıcının bir yorumu beğenmesi. Aynı kişi aynı yorumu bir kez beğenebilir.
    public class ReviewLike
    {
        public int Id { get; set; }

        public int ReviewId { get; set; }
        public Review Review { get; set; } = null!;

        public string UserId { get; set; } = "";
        public IdentityUser User { get; set; } = null!;
    }
}
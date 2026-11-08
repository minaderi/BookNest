using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BookNest.Models
{
    // Yıllık okuma hedefi: kullanıcı bir yıl içinde kaç kitap okumak istediğini yazar.
    // Her kullanıcının her yıl için tek bir hedefi olur.
    public class ReadingGoal
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int Year { get; set; }

        [Range(1, 1000)]
        public int Target { get; set; }
    }
}

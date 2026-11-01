using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace BookNest.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; }
    }
}
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookNest.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public ICollection<UserBook> UserBooks { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
}
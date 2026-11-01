using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BookNest.Models
{
    public class UserBook
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } // "Read", "CurrentlyReading", "WantToRead"

        public bool IsFavorite { get; set; } = false;

        public DateTime? ReadDate { get; set; }
    }
}
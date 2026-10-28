using System;
using System.ComponentModel.DataAnnotations;

namespace BookNest.Models
{
    public class UserBook
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } // "Read", "CurrentlyReading", "WantToRead"

        public bool IsFavorite { get; set; } = false;

        public DateTime? ReadDate { get; set; }
    }
}
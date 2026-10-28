using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookNest.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(150)]
        public string Author { get; set; }

        public string Description { get; set; }

        public string CoverImageUrl { get; set; }

        public int PublishYear { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public ICollection<UserBook> UserBooks { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
}
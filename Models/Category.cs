using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookNest.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public ICollection<Book> Books { get; set; }
    }
}
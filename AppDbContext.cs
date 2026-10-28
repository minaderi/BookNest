using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BookNest.Models;
using Microsoft.AspNetCore.Identity;

namespace BookNest.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> AppUsers { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserBook> UserBooks { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Kategoriyi otomatik ekle
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Fiction" }
            );

            // Kitapları otomatik ekle
            modelBuilder.Entity<Book>().HasData(
                new Book 
                { 
                    Id = 1, 
                    Title = "The End of Everything", 
                    Author = "M. John Harrison", 
                    Description = "Booker Shortlist 2026 Adayı", 
                    CoverImageUrl = "/images/end-of-everything.jpg", 
                    PublishYear = 2026, 
                    CategoryId = 1 
                },
                new Book 
                { 
                    Id = 2, 
                    Title = "The Disappearers", 
                    Author = "Marlon James", 
                    Description = "Booker Shortlist 2026 Adayı", 
                    CoverImageUrl = "/images/disappearers.jpg", 
                    PublishYear = 2026, 
                    CategoryId = 1 
                },
                new Book 
                { 
                    Id = 3, 
                    Title = "Black Bag", 
                    Author = "Luke Kennard", 
                    Description = "Booker Shortlist 2026 Adayı", 
                    CoverImageUrl = "/images/black-bag.jpg", 
                    PublishYear = 2026, 
                    CategoryId = 1 
                }
            );
        }
    }
}
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

        public DbSet<Book> Books { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserBook> UserBooks { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewLike> ReviewLikes { get; set; }
        public DbSet<ReviewReply> ReviewReplies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.BookId })
                .IsUnique();

            // Yorum beğenileri: bir kullanıcı bir yorumu en fazla bir kez beğenebilir
            modelBuilder.Entity<ReviewLike>()
                .HasIndex(l => new { l.ReviewId, l.UserId })
                .IsUnique();

            // Yorum silinirse beğenileri de silinir
            modelBuilder.Entity<ReviewLike>()
                .HasOne(l => l.Review)
                .WithMany()
                .HasForeignKey(l => l.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            // SQL Server aynı tabloya iki ayrı silme zinciri kabul etmediği için
            // kullanıcı tarafında otomatik silme kapalı
            modelBuilder.Entity<ReviewLike>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Yorum yanıtları: yorum silinirse yanıtları da silinir
            modelBuilder.Entity<ReviewReply>()
                .HasOne(r => r.Review)
                .WithMany()
                .HasForeignKey(r => r.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            // Beğenilerdeki gibi, kullanıcı tarafında otomatik silme kapalı
            modelBuilder.Entity<ReviewReply>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReviewReply>()
                .Property(r => r.Text)
                .HasMaxLength(1000);

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
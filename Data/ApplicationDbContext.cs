using Microsoft.EntityFrameworkCore;
using Recipe_Sharing_Platform_API.Models;

namespace Recipe_Sharing_Platform_API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Receipt> Recipes => Set<Receipt>();
        public DbSet<Like> Likes => Set<Like>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(b =>
            {
                b.HasIndex(u => u.Username).IsUnique();
                b.Property(u => u.Username).HasMaxLength(30).IsRequired();
            });

            builder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Like>()
                .HasOne(l => l.Receipt)
                .WithMany(r => r.Likes)
                .HasForeignKey(l => l.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
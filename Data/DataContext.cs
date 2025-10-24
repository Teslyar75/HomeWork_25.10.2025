using ASP_421.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ASP_421.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Entities.User> Users { get; set; }
        public DbSet<Entities.UserAccess> UserAccesses { get; set; }
        public DbSet<Entities.UserRole> UserRoles { get; set; }
        public DbSet<Entities.ProductGroup> ProductGroups { get; set; }
        public DbSet<Entities.Product> Products { get; set; }
        public DbSet<Entities.CartItem> CartItems { get; set; }
        public DataContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserAccessConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.Entity<Entities.Product>()
                .HasIndex(p => p.Slug)
                .IsUnique();
            modelBuilder.Entity<Entities.Product>()
                .HasOne(p => p.Group)
                .WithMany(g => g.Products)
                .HasForeignKey(p => p.GroupId);
            modelBuilder.Entity<Entities.ProductGroup>()
                .HasIndex(g => g.Slug)
                .IsUnique();

            // Конфигурация для CartItem
            modelBuilder.Entity<Entities.CartItem>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Entities.CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Уникальный индекс для предотвращения дублирования товаров в корзине
            modelBuilder.Entity<Entities.CartItem>()
                .HasIndex(c => new { c.UserId, c.ProductId })
                .IsUnique();

        }
    }
}

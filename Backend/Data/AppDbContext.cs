using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<AppUser>().Property(u => u.IsActive).HasDefaultValue(true);
        mb.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
        mb.Entity<Product>().Property(p => p.DiscountPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        mb.Entity<Order>().Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
        mb.Entity<Order>().Property(o => o.DeliveryFee).HasColumnType("decimal(18,2)");
        mb.Entity<Order>().Property(o => o.TaxAmount).HasColumnType("decimal(18,2)");
        mb.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        mb.Entity<Order>().Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
        mb.Entity<OrderItem>().Property(o => o.UnitPrice).HasColumnType("decimal(18,2)");
        mb.Entity<Cart>().Property(c => c.BudgetLimit).HasColumnType("decimal(18,2)");
        mb.Entity<AppUser>().HasOne(u => u.Cart).WithOne(c => c.Customer)
            .HasForeignKey<Cart>(c => c.CustomerId);
        mb.Entity<Review>().Property(r => r.Rating).IsRequired();
        mb.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        mb.Entity<Coupon>().Property(c => c.DiscountValue).HasColumnType("decimal(18,2)");
        mb.Entity<Coupon>().Property(c => c.MinOrderAmount).HasColumnType("decimal(18,2)");
    }
}

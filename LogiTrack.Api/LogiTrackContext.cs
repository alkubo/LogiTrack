using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LogiTrack.Api.Models;

namespace LogiTrack.Api;

public class LogiTrackContext : IdentityDbContext<ApplicationUser>
{
    public LogiTrackContext(DbContextOptions<LogiTrackContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Performance index for frequent filtering / searching by customer
        modelBuilder.Entity<Order>().HasIndex(o => o.CustomerName).HasDatabaseName("IX_Orders_CustomerName");
    }
}

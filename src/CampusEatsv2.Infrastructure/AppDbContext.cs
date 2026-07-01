using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using CampusEatsv2.Core.Models;

namespace CampusEatsv2.Infrastructure;

// This  is the heart of the application
// DBcontext makes NUnit tests work and makes a db that we can use without spin up in dev
// By using Factory we can convert this into a real db during spin up of docker.

public class AppDbContext : DbContext
{
    // DB tables for different entities and services
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Courier> Couriers { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Payment> Payments { get; set; }

    // Base options, that can be used for manual seeding and logging for debug
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Override to fix issues with DateTime -> UTCDateTime
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc
                ? v
                : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
        );

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dateTimeProperties = entityType
                .GetProperties()
                .Where(p => p.ClrType == typeof(DateTime));

            foreach (var property in dateTimeProperties)
            {
                property.SetValueConverter(utcConverter);
            }
        }
    }
}

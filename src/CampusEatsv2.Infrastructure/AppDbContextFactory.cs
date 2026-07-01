using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
 
namespace CampusEatsv2.Infrastructure;

// This factory is used by EF Core tools (like migrations) to create an instance of AppDbContext at design time,
// when the application isn't running and dependency injection isn't available. So this is just for production, not tests.

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // This method is called by EF Core tools to create a new instance of AppDbContext.
    public AppDbContext CreateDbContext(string[] args)
    {
        // Here we configure the DbContextOptions to use PostgreSQL with a connection string.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=campuseatsdb;Username=campuseats;Password=changeme")
            .Options;
        
        // Finally, we return a new instance of AppDbContext with the configured options. So that it can be used by EF Core tools
        return new AppDbContext(options);
    }
}
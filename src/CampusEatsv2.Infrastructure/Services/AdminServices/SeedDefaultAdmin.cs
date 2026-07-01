using CampusEatsv2.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CampusEatsv2.Infrastructure.Services.AdminServices;

public class SeedDefaultAdmin
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        // Retrieve a logger instance
        var logger = serviceProvider.GetRequiredService<ILogger<SeedDefaultAdmin>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        // Retrieve the database context
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>(); 

        const string defaultEmail = "admin@campuseats.com";
        const string defaultPassword = "admin1234"; 

        // Check if a default admin already exists
        if (dbContext.Admins.Any(a => a.Email == defaultEmail))
        {
            logger.LogInformation("Default admin already exists. Skipping seeding.");
            return;
        }

        var passwordHasher = new PasswordHasher<Admin>();

        var defaultAdmin = new Admin
        {
            AdminId = Guid.NewGuid(),
            Email = defaultEmail,
            PasswordHash = string.Empty,
            IsFirstLogin = false,
            InvitedByAdminId = null // No inviting admin for the root admin
        };

        defaultAdmin.PasswordHash = passwordHasher.HashPassword(defaultAdmin, defaultPassword);

        await dbContext.Admins.AddAsync(defaultAdmin);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Default admin seeded successfully with email: {Email}", defaultEmail);
    }
}
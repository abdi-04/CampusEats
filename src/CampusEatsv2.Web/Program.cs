using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Messaging;
using CampusEatsv2.Infrastructure.Services.AdminServices;
using CampusEatsv2.Infrastructure.Services.Behaviors;
using CampusEatsv2.Infrastructure.Services.OrderServices;
using CampusEatsv2.Infrastructure.Services.SharedServices;
using CampusEatsv2.Infrastructure.Services.Seeding;
using CampusEatsv2.Web.Components;
using CampusEatsv2.Core.Interfaces;
using CampusEatsv2.Web.Components.Notifications;


using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

namespace CampusEatsv2.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Decide database mode based ONLY on presence of connection string
        var connectionString =
            builder.Configuration.GetConnectionString("DefaultConnection");

        var useInMemory = string.IsNullOrWhiteSpace(connectionString);

        ConfigureServices(builder.Services, builder.Configuration, connectionString, useInMemory);

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        // DATABASE MIGRATION + SEEDING 
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<AppDbContext>();

            if (!useInMemory)
            {
                await db.Database.MigrateAsync();
            }

            // Safe to run always (seeders already guard against duplicates)
            await SeedDefaultAdmin.SeedAsync(services);
            await SeedTestData.SeedAsync(services);
        }

        app.UseStatusCodePagesWithReExecute(
            "/not-found",
            createScopeForStatusCodePages: true);

        // Avoid HTTPS redirect issues inside Docker
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseAntiforgery();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Admin promotion endpoint
        app.MapPost("/admin/promote",
            async (InviteAdminUser.InviteAdminUserCommand command,
                   IMediator mediator) =>
            {
                var admin = await mediator.Send(command);
                return Results.Created($"/admin/{admin.AdminId}", admin);
            });
            app.MapHub<NotificationHub>("/hubs/notifications");

        // 1) START Google login
        app.MapGet("/auth/google", async (HttpContext ctx, string? returnUrl) =>
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/signin-google-handler"
            };

            await ctx.ChallengeAsync("Google", props);
        });

        // 2) HANDLE Google callback
        app.MapGet("/signin-google-handler", async (HttpContext ctx) =>
        {
            var result = await ctx.AuthenticateAsync("Cookies");

            if (!result.Succeeded)
                return Results.Redirect("/login?error=google_failed");

            var email = result.Principal?
                .FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
                return Results.Redirect("/login?error=no_email");

            var name = result.Principal?
                .FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";

            // Redirect to role selector instead of straight to dashboard
            return Results.Redirect(
                $"/google-role?email={Uri.EscapeDataString(email)}&name={Uri.EscapeDataString(name)}");
        });
        app.Run();
    }

    // Used by tests
    public static ServiceProvider ConfigureServices(
        bool useInMemory = false,
        string? connectionString = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        ConfigureServices(
            services,
            configuration,
            connectionString,
            useInMemory);

        return services.BuildServiceProvider();
    }


    // Main DI configuration
    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        string? connectionString = null,
        bool useInMemory = false)
    {
        if (useInMemory)
        {
            services.AddDbContext<AppDbContext>(db =>
                db.UseInMemoryDatabase("TestDb"));
        }
        else
        {
            services.AddDbContext<AppDbContext>(db =>
                db.UseNpgsql(
                    connectionString
                    ?? throw new InvalidOperationException(
                        "No connection string provided.")));
            services.AddDataProtection()
                .PersistKeysToFileSystem(
                new DirectoryInfo("/root/.aspnet/DataProtection-Keys")
                );
        }

        services.AddLogging();

        // Registering Google Auth services
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "Google";
        })
        .AddCookie("Cookies")
        .AddGoogle("Google", options =>
        {
            var google = configuration.GetSection("Authentication:Google");

            options.ClientId = google["ClientId"]
                ?? throw new InvalidOperationException(
                    "Authentication:Google:ClientId not configured");

            options.ClientSecret = google["ClientSecret"]
                ?? throw new InvalidOperationException(
                    "Authentication:Google:ClientSecret not configured");

            options.CallbackPath = "/signin-google";
        });


        // App auth state (Blazor)
        services.AddSingleton<AuthenticationStateService>();

        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // MediatR registrations
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(PlaceOrderService).Assembly);

            cfg.RegisterServicesFromAssembly(
                typeof(InviteAdminUser).Assembly);

            
          //Domain event handlers (Infrastructure)
            cfg.RegisterServicesFromAssembly(
                typeof(CustomerRegisteredDomainEventHandler).Assembly);
        });

        services.AddSingleton<IEventBus, RabbitMqEventBus>();
                //Add SignalR
        services.AddSignalR();

        services.AddSingleton<INotificationStore, NotificationStore>();
        services.AddScoped<INotificationSender, SignalRNotificationSender>();

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationPipelineBehavior<,>));

        services.AddValidatorsFromAssembly(
            typeof(PlaceOrderService).Assembly);
    }
}

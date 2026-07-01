using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CampusEatsv2.Infrastructure.Services.CustomerServices.LoginOrRegisterGoogleCustomer;

namespace CampusEatsv2.Tests;

// <summary>
// Tests for the LoginOrRegisterGoogleCustomer command handler.
// Covers both the "login" and "register" paths, as well as ensuring no duplicates are created.

[TestFixture]
public class LoginOrRegisterGoogleCustomerTests
{
    // null!:
    // 1) Suppresses nullable warnings.
    // 2) Fields are assigned in [SetUp].
    private IMediator _mediator = null!;
    private AppDbContext _context = null!;

    // Helper method to create a customer in the database with specified email and name.
    // Why this exists:
    // Avoids repeating seed logic in every test.
    // Allows customizing:
    // Email
    // FullName
    private async Task<Customer> CreateCustomerAsync(
        string email = "google@test.com",
        string fullName = "Existing User")
    {
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = "GOOGLE_OAUTH"
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    // setup method runs before each test. It:
    // 1. Configures services to use an in-memory database.
    // 2. Retrieves the IMediator and AppDbContext instances for use in tests.
    [SetUp]
    public void Setup()
    {
        var serviceProvider = Program.ConfigureServices(useInMemory: true);

        _mediator = serviceProvider.GetRequiredService<IMediator>();
        _context = serviceProvider.GetRequiredService<AppDbContext>();

        _context.Database.EnsureCreated();
    }

    // tear down method runs after each test. It:
    // 1. Deletes the in-memory database to ensure a clean state for the next test
    // 2. Disposes the database context to free resources.
    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task LoginOrRegisterGoogleCustomer_NewUser_CreatesAndReturnsCustomer()
    {
        var command = new Command
        {
            Email = "newuser@test.com",
            Name = "New User"
        };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo("newuser@test.com"));
        Assert.That(result.FullName, Is.EqualTo("New User"));
        Assert.That(result.PasswordHash, Is.EqualTo("GOOGLE_OAUTH"));
    }

    [Test]
    public async Task LoginOrRegisterGoogleCustomer_NewUser_IsPersisted()
    {
        var command = new Command
        {
            Email = "newuser@test.com",
            Name = "New User"
        };

        await _mediator.Send(command);

        var inDb = _context.Customers.FirstOrDefault(c => c.Email == "newuser@test.com");
        Assert.That(inDb, Is.Not.Null);
    }

    [Test]
    public async Task LoginOrRegisterGoogleCustomer_ExistingUser_ReturnsExistingCustomer()
    {
        var seeded = await CreateCustomerAsync(
            email: "google@test.com",
            fullName: "Existing User");

        var command = new Command
        {
            Email = "google@test.com",
            Name = "Existing User"
        };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.CustomerId, Is.EqualTo(seeded.CustomerId));
        Assert.That(result.Email, Is.EqualTo(seeded.Email));
        Assert.That(result.FullName, Is.EqualTo(seeded.FullName));
    }

    [Test]
    public async Task LoginOrRegisterGoogleCustomer_ExistingUser_DoesNotCreateDuplicate()
    {
        await CreateCustomerAsync(email: "google@test.com");

        var command = new Command
        {
            Email = "google@test.com",
            Name = "Existing User"
        };

        await _mediator.Send(command);

        var count = _context.Customers.Count(c => c.Email == "google@test.com");
        Assert.That(count, Is.EqualTo(1));
    }
}
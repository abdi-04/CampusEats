using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CampusEatsv2.Infrastructure.Services.CustomerServices.LoginCustomer;

namespace CampusEatsv2.Tests;

[TestFixture]
public class LoginCustomerTests
{
    private async Task<Customer> CreateLoginCustomerAsync(
        string email = "login@test.com",
        string plainPassword = "test12345")
    {
        var hasher = new PasswordHasher<Customer>();
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            FullName = "Test User",
            Email = email,
            PasswordHash = ""
        };

        customer.PasswordHash = hasher.HashPassword(customer, plainPassword);

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }


    private IMediator _mediator = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        var serviceProvider = Program.ConfigureServices(useInMemory: true);

        _mediator = serviceProvider.GetRequiredService<IMediator>();
        _context = serviceProvider.GetRequiredService<AppDbContext>();

        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task LoginCustomer_ValidCredentials_ReturnsCustomer()
    {
        // Create existing customer in db
        var seeded = await CreateLoginCustomerAsync(
            email: "login@test.com",
            plainPassword: "test12345");

        var command = new LoginCustomerCommand
        {
            Email = "login@test.com",
            Password = "test12345"
        };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.CustomerId, Is.EqualTo(seeded.CustomerId));
        Assert.That(result.Email, Is.EqualTo(seeded.Email));
        Assert.That(result.FullName, Is.EqualTo(seeded.FullName));
    }
    [Test]
    public async Task LoginCustomer_WrongEmail_ThrowsValidationException()
    {
        await CreateLoginCustomerAsync(email: "login@test.com");

        var command = new LoginCustomerCommand
        {
            Email = "nonexisting@test.com",
            Password = "test12345"
        };

        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("Invalid email or password"));
    }
    [Test]
    public async Task LoginCustomer_WrongPassword_ThrowsValidationException()
    {
        await CreateLoginCustomerAsync(
                email: "login@test.com",
                plainPassword: "test12345");

        var command = new LoginCustomerCommand
        {
            Email = "login@test.com",
            Password = "invalid-password"  
        };

        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("Invalid email or password"));
   }
 
}

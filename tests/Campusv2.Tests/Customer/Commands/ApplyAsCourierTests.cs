using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CampusEatsv2.Infrastructure.Services.CustomerServices.ApplyAsCourier;

namespace CampusEatsv2.Tests;

[TestFixture]
public class ApplyAsCourierTests
{    
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

    public async Task<Courier> CreateCourierAsync(
        string email = "courier@test.com")
    {
        var courier = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Test Courier",
            Email = email,
            PasswordHash = "hashedpassword",
            Status = CourierStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Couriers.Add(courier);
        await _context.SaveChangesAsync();
        return courier;
    }

    [Test]
    public async Task CreateCourierAsync_Returns_Courier()
    {
        // Arrange
        var email = "courier@test.com";

        // Act
        var courier = await CreateCourierAsync(email);

        // Assert
        Assert.NotNull(courier);
        Assert.AreEqual(email, courier.Email);
    }

    [Test]
    public async Task ApplyAsCourier_Success()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            FullName = "Test User",
            Email = "user@test.com",
            PasswordHash = "hashedpassword"
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
    

        var command = new ApplyAsCourierCommand
        {
            CustomerId = customer.CustomerId
        };

        // Act
        var courier = await _mediator.Send(command);

        // Assert
        Assert.NotNull(courier);
        Assert.AreEqual(customer.Email, courier.Email);
        Assert.AreEqual(CourierStatus.Pending, courier.Status);

    }
        
    [Test]
    public async Task ApplyAsCourier_DuplicateApplication_ThrowsException()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            FullName = "Test User",
            Email = "user@test.com",
            PasswordHash = "hashedpassword"
        };
    
        _context.Customers.Add(customer);
    
        // Existing courier with same email
        _context.Couriers.Add(new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = customer.FullName,
            Email = customer.Email,
            PasswordHash = customer.PasswordHash,
            Status = CourierStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
    
        await _context.SaveChangesAsync();
    
        var command = new ApplyAsCourierCommand
        {
            CustomerId = customer.CustomerId
        };
    
        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _mediator.Send(command);
        });
    }

}
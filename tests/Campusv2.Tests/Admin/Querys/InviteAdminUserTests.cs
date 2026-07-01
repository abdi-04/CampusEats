using CampusEatsv2.Core.Models;
using AdminModel = CampusEatsv2.Core.Models.Admin;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.AdminServices;
using CampusEatsv2.Web;
using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class InviteAdminUserTests
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

    private async Task<Customer> CreateCustomer()
    {
        var hasher = new PasswordHasher<Customer>();
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            FullName = "Test Customer",
            Email = "customer@example.com",
            PasswordHash = hasher.HashPassword(new Customer { FullName = "Test Customer", Email = "customer@example.com", PasswordHash = "" }, "customerPassword123")
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }
    
    private async Task<Courier> CreateCourier(CourierStatus status = CourierStatus.Approved)
    {
        var hasher = new PasswordHasher<Courier>();
        var courier = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Test Courier",
            Email = "courier@example.com",
            PasswordHash = hasher.HashPassword(new Courier { FullName = "Test Courier", Email = "courier@example.com", PasswordHash = "" }, "courierPassword123"),
            Status = status
        };

        _context.Couriers.Add(courier);
        await _context.SaveChangesAsync();
        return courier;
    }

    [Test]
    public async Task InviteAdminUser_Customer_Succeeds()
    {
        var customer = await CreateCustomer();

        var command = new InviteAdminUser.InviteAdminUserCommand
        {
            SourceType = InviteAdminUser.SourceType.Customer,
            SourceId = customer.CustomerId,
            InitialPassword = "adminTemp123"
        };

        var admin = await _mediator.Send(command);

        Assert.That(admin, Is.Not.Null);
        Assert.That(admin.Email, Is.EqualTo(customer.Email));
        Assert.That(admin.IsFirstLogin, Is.True);

        var saved = await _context.Admins.FindAsync(admin.AdminId);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Email, Is.EqualTo(customer.Email));
    }

    [Test]
    public async Task InviteAdminUser_Courier_Succeeds()
    {
        var courier = await CreateCourier();

        var command = new InviteAdminUser.InviteAdminUserCommand
        {
            SourceType = InviteAdminUser.SourceType.Courier,
            SourceId = courier.CourierId,
            InitialPassword = "adminTemp123"
        };

        var admin = await _mediator.Send(command);

        Assert.That(admin, Is.Not.Null);
        Assert.That(admin.Email, Is.EqualTo(courier.Email));
        Assert.That(admin.IsFirstLogin, Is.True);
    }

    [Test]
    public void InviteAdminUser_SourceNotFound_ThrowsKeyNotFoundException()
    {
        var command = new InviteAdminUser.InviteAdminUserCommand
        {
            SourceType = InviteAdminUser.SourceType.Customer,
            SourceId = Guid.NewGuid(),
            InitialPassword = "adminTemp123"
        };

        var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () => await _mediator.Send(command));
        Assert.That(ex!.Message, Does.Contain("Customer not found"));
    }

    [Test]
    public async Task InviteAdminUser_DuplicateAdminEmail_ThrowsInvalidOperationException()
    {
        var customer = await CreateCustomer();

        var hasher = new PasswordHasher<AdminModel>();
        _context.Admins.Add(new AdminModel
        {
            AdminId = Guid.NewGuid(),
            Email = customer.Email,
            PasswordHash = hasher.HashPassword(new AdminModel { Email = customer.Email, PasswordHash = "" }, "existingPassword123"),
            IsFirstLogin = false
        });
        await _context.SaveChangesAsync();

        var command = new InviteAdminUser.InviteAdminUserCommand
        {
            SourceType = InviteAdminUser.SourceType.Customer,
            SourceId = customer.CustomerId,
            InitialPassword = "adminTemp123"
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _mediator.Send(command));
        Assert.That(ex!.Message, Does.Contain("already an admin"));
    }

    [Test]
    public void InviteAdminUser_WeakPassword_ReturnsValidationError()
    {
        var command = new InviteAdminUser.InviteAdminUserCommand
        {
            SourceType = InviteAdminUser.SourceType.Courier,
            SourceId = Guid.NewGuid(),
            InitialPassword = "short"
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () => await _mediator.Send(command));
        Assert.That(ex!.Errors.Any(e => e.PropertyName == "InitialPassword"));
    }
}
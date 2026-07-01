using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Web;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CampusEatsv2.Infrastructure.Services.CourierServices.LoginCourier;

namespace CampusEatsv2.Tests;

[TestFixture]
public class LoginTests
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

    private async Task<Courier> CreateTestCourierAsync(
        string email = "courier@example.com",
        string plainTextPassword = "Password123!",
        CourierStatus status = CourierStatus.Approved)
    {
        var hasher = new PasswordHasher<Courier>();
        var courier = new Courier
        {
            CourierId = Guid.NewGuid(),
            FullName = "Courier Test",
            Email = email,
            PasswordHash = hasher.HashPassword(new Courier { FullName = "Courier Test", Email = email, PasswordHash = "" }, plainTextPassword),
            Status = status
        };

        _context.Couriers.Add(courier);
        await _context.SaveChangesAsync();
        _context.Entry(courier).State = EntityState.Detached;

        return courier;
    }

    [Test]
    public async Task CourierService_LoginCourier_ApprovedCourier_ReturnsCourier()
    {
        var plainTextPassword = "Password123!";
        var courier = await CreateTestCourierAsync(plainTextPassword: plainTextPassword, status: CourierStatus.Approved);

        var command = new LoginCourierCommand
        {
            Email = courier.Email,
            Password = plainTextPassword,
            Status = CourierStatus.Approved
        };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.CourierId, Is.EqualTo(courier.CourierId));
        Assert.That(result.Email, Is.EqualTo(courier.Email));
        Assert.That(result.Status, Is.EqualTo(CourierStatus.Approved));
    }

    [Test]
    public void CourierService_LoginCourier_InvalidPassword_ThrowsValidationException()
    {
        var courier = CreateTestCourierAsync().GetAwaiter().GetResult();

        var command = new LoginCourierCommand
        {
            Email = courier.Email,
            Password = "WrongPassword",
            Status = CourierStatus.Approved
        };

        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("Invalid email or password."));
    }

    [Test]
    public void CourierService_LoginCourier_UnapprovedCourier_ThrowsValidationException()
    {
        var courier = CreateTestCourierAsync(status: CourierStatus.Pending).GetAwaiter().GetResult();

        var command = new LoginCourierCommand
        {
            Email = courier.Email,
            Password = "Password123!",
            Status = CourierStatus.Pending
        };

        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("Courier registration is not approved yet."));
    }

    [Test]
    public void CourierService_LoginCourier_InvalidEmailFormat_ThrowsValidationException()
    {
        var command = new LoginCourierCommand
        {
            Email = "not-an-email",
            Password = "Password123!",
            Status = CourierStatus.Approved
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "Email"), Is.True);
    }
}

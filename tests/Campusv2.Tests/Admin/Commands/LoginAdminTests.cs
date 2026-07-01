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
public class LoginAdminTests
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

    // Helper method to create admin
    private async Task<AdminModel> CreateAdmin(
        string email = "admin@example.com",
        string password = "Admin123!",
        bool isFirstLogin = true)
    {
        var hasher = new PasswordHasher<AdminModel>();

        var admin = new AdminModel
        {
            AdminId = Guid.NewGuid(),
            Email = email,
            PasswordHash = hasher.HashPassword(
                new AdminModel { Email = email, PasswordHash = "" },
                password),
            IsFirstLogin = isFirstLogin
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();
        return admin;
    }

    [Test]
    public async Task LoginAdmin_ValidCredentials_ReturnsAdminResult()
    {
        var password = "Admin123!";
        var admin = await CreateAdmin(password: password);

        var command = new LoginAdmin.LoginAdminCommand
        {
            Email = admin.Email,
            Password = password
        };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Admin.AdminId, Is.EqualTo(admin.AdminId));
        Assert.That(result.Admin.Email, Is.EqualTo(admin.Email));
        Assert.That(result.RequiresPasswordChange, Is.True);
    }

    [Test]
    public void LoginAdmin_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        var admin = CreateAdmin().GetAwaiter().GetResult();

        var command = new LoginAdmin.LoginAdminCommand
        {
            Email = admin.Email,
            Password = "WrongPassword"
        };

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public void LoginAdmin_AdminNotFound_ThrowsUnauthorizedAccessException()
    {
        var command = new LoginAdmin.LoginAdminCommand
        {
            Email = "notfound@example.com",
            Password = "Admin123!"
        };

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mediator.Send(command));
    }

    [Test]
    public async Task LoginAdmin_FirstLoginFalse_ReturnsRequiresPasswordChangeFalse()
    {
        var password = "Admin123!";
        var admin = await CreateAdmin(password: password, isFirstLogin: false);

        var command = new LoginAdmin.LoginAdminCommand
        {
            Email = admin.Email,
            Password = password
        };

        var result = await _mediator.Send(command);

        Assert.That(result.RequiresPasswordChange, Is.False);
    }

    [Test]
    public void LoginAdmin_InvalidEmailFormat_ThrowsValidationException()
    {
        var command = new LoginAdmin.LoginAdminCommand
        {
            Email = "not-an-email",
            Password = "Admin123!"
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "Email"), Is.True);
    }

        [Test]
    public void LoginAdmin_EmptyFields_ThrowsValidationException()
    {
        var command = new LoginAdmin.LoginAdminCommand
        {
            Email = "",
            Password = ""
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(), Is.True);
    }
}



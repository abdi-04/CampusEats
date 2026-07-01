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
public class ForcePasswordChangeTests
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

    // - Helper 
    private async Task<CampusEatsv2.Core.Models.Admin> CreateAdmin(bool isFirstLogin)
    {
        var hasher = new PasswordHasher<CampusEatsv2.Core.Models.Admin>();
        var admin = new CampusEatsv2.Core.Models.Admin
        {
            AdminId = Guid.NewGuid(),
            //FullName = "Test Admin",
            Email = "admin@example.com",
            PasswordHash = hasher.HashPassword(new CampusEatsv2.Core.Models.Admin { Email = "admin@example.com", PasswordHash = "" }, "oldpassword123"),
            IsFirstLogin = isFirstLogin
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();
        return admin;
    }

    // - Tests 
    [Test]
    // Valid admin with IsFirstLogin = true should successfully change password
    public async Task ForcePasswordChange_ValidAdmin_ChangesPasswordSuccessfully()
    {
        var admin = await CreateAdmin(isFirstLogin: true);

        var command = new ForcePasswordChangeAdmin.ForcePasswordChangeCommand
        {
            AdminId = admin.AdminId,
            NewPassword = "newpassword123",
        };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsFirstLogin, Is.False); // flag must be cleared
    }

    [Test]
    // New password should be hashed, not stored as plain text
    public async Task ForcePasswordChange_ValidAdmin_PasswordIsHashed()
    {
        var admin = await CreateAdmin(isFirstLogin: true);

        var command = new ForcePasswordChangeAdmin.ForcePasswordChangeCommand
        {
            AdminId = admin.AdminId,
            NewPassword = "newpassword123",
        };

        var result = await _mediator.Send(command);

        // Must not store plain text
        Assert.That(result.PasswordHash, Is.Not.EqualTo("newpassword123"));

        // Hash must verify correctly against the new password
        var hasher = new PasswordHasher<CampusEatsv2.Core.Models.Admin>();
        var verification = hasher.VerifyHashedPassword(result, result.PasswordHash, "newpassword123");
        Assert.That(verification, Is.EqualTo(PasswordVerificationResult.Success));
    }


    [Test]
    // Admin with IsFirstLogin = false should not be allowed to change password
    public async Task ForcePasswordChange_IsFirstLoginFalse_ThrowsException()
    {
        var admin = await CreateAdmin(isFirstLogin: false);

        var command = new ForcePasswordChangeAdmin.ForcePasswordChangeCommand
        {
            AdminId = admin.AdminId,
            NewPassword = "newpassword123",
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("first login"));
    }


    [Test]
    // Password under 8 characters should fail validation
    public void ForcePasswordChange_ShortPassword_ReturnsError()
    {
        var command = new ForcePasswordChangeAdmin.ForcePasswordChangeCommand
        {
            AdminId = Guid.NewGuid(),
            NewPassword = "short",
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "NewPassword"), Is.True);
    }

    [Test]
    // Password over 32 characters should fail validation
    public void ForcePasswordChange_LongPassword_ReturnsError()
    {
        var command = new ForcePasswordChangeAdmin.ForcePasswordChangeCommand
        {
            AdminId = Guid.NewGuid(),
            NewPassword = "this-password-is-way-too-long-and-should-fail",
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "NewPassword"), Is.True);
    }

    [Test]
    // Empty AdminId should fail validation
    public void ForcePasswordChange_EmptyAdminId_ReturnsError()
    {
        var command = new ForcePasswordChangeAdmin.ForcePasswordChangeCommand
        {
            AdminId = Guid.Empty,
            NewPassword = "newpassword123",
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "AdminId"), Is.True);
    }

    [Test]
    // Admin that does not exist should throw KeyNotFoundException
    public void ForcePasswordChange_AdminNotFound_ThrowsException()
    {
        var command = new ForcePasswordChangeAdmin.ForcePasswordChangeCommand
        {
            AdminId = Guid.NewGuid(), // does not exist in DB
            NewPassword = "newpassword123",
        };

        var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Does.Contain("Admin not found"));
    }
}
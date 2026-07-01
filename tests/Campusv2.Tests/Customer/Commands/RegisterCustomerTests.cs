using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.CustomerServices;
using CampusEatsv2.Infrastructure.Messaging;
using CampusEatsv2.Infrastructure.Services.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class RegisterCustomerTests
{
    private async Task<Customer> CreateCustomerInDbAsync(
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
        var services = new ServiceCollection();
    
        // REQUIRED: Logging must be registered BEFORE MediatR
        services.AddLogging();
    
        // In-memory EF Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        
        services.AddValidatorsFromAssemblyContaining<RegisterCustomer.Validator>();
    
        //  MediatR (commands + domain events)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(RegisterCustomer.RegisterCustomerHandler).Assembly);
    
            cfg.RegisterServicesFromAssembly(
                typeof(CustomerRegisteredDomainEventHandler).Assembly);
        });

        
        // Validation pipeline (THIS WAS MISSING)
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationPipelineBehavior<,>));
    
        // Fake event bus (prevents RabbitMQ in tests)
        services.AddSingleton<IEventBus, FakeEventBus>();
    
        var provider = services.BuildServiceProvider();
    
        _mediator = provider.GetRequiredService<IMediator>();
        _context = provider.GetRequiredService<AppDbContext>();
    
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        if (_context is not null)
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }

    [Test]
    public async Task RegisterCustomer_ValidData_CreatesCustomer()
    {
        var command = new RegisterCustomer.RegisterCustomerCommand
        {
            FullName = "Test User",
            Email = "new@test.com",
            Password = "test12345",
            Address = "Test"
        };

        var result = await _mediator.Send(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo(command.Email));
        Assert.That(result.FullName, Is.EqualTo(command.FullName));

        // Verify it is in DB
        var customerInDb = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == command.Email);

        Assert.That(customerInDb, Is.Not.Null);
    }

    [Test]
    public async Task RegisterCustomer_EmailAlreadyExists_ThrowsException()
    {
        await CreateCustomerInDbAsync(email: "test@test.com");

        var command = new RegisterCustomer.RegisterCustomerCommand
        {
            FullName = "Test User",
            Email = "test@test.com",
            Password = "test12345"
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Message, Is.EqualTo("Email is already in use"));
    }

    [Test]
    public void RegisterCustomer_InvalidEmail_ThrowsValidationException()
    {
        var command = new RegisterCustomer.RegisterCustomerCommand
        {
            FullName = "Test User",
            Email = "invalid-email",
            Password = "test12345"
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(command));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "Email"));
    }
    [Test]
    public async Task RegisterCustomer_PasswordIsHashed()
    {
        var command = new RegisterCustomer.RegisterCustomerCommand
        {
            FullName = "Test User",
            Email = "hash@test.com",
            Password = "test12345"
        };

        var result = await _mediator.Send(command);

        Assert.That(result.PasswordHash, Is.Not.EqualTo(command.Password));

        var hasher = new PasswordHasher<Customer>();
        var verification = hasher.VerifyHashedPassword(
            result,
            result.PasswordHash,
            command.Password);

        Assert.That(verification, Is.EqualTo(PasswordVerificationResult.Success));
    }
}
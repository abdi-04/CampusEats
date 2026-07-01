using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.SharedServices;
using CampusEatsv2.Web;
using MediatR;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class ProductServiceTests
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

    [Test]
    // Check all data to be valid
    public async Task ProductService_ValidData_ReturnsResult()
    {
        var product = new ProductService.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "Test Product",
            Description = "This is a test product",
            Price = 10,
            ImageUrl = "http://example.com/image.jpg"
        };

        var result = await _mediator.Send(product);

        Assert.That(product.ProductId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(product.Title, Is.EqualTo("Test Product"));
        Assert.That(product.Description, Is.EqualTo("This is a test product"));
        Assert.That(product.Price, Is.EqualTo(10));
        Assert.That(product.ImageUrl, Is.EqualTo("http://example.com/image.jpg"));
        Assert.That(result, Is.Not.EqualTo(0));
    }

    [Test]
    // Test if Title is empty
    // Check should return error on Title property
    public void ProductService_EmptyTitle_ReturnError()
    {
        var product = new ProductService.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "",
            Description = "This is a test product",
            Price = 10,
            ImageUrl = "http://example.com/image.jpg"
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(product));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "Title"), Is.True);
    }

    [Test]
    // Test if Price is negative
    // Check should return error on Price property
    public void ProductService_NegativePrice_ReturnError()
    {
        var product = new ProductService.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "Test Product",
            Description = "This is a test product",
            Price = -5,
            ImageUrl = "http://example.com/image.jpg"
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(product));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "Price"), Is.True);
    }

    [Test]
    // Test if Price is zero
    // Check should return error on Price property
    public void ProductService_ZeroPrice_ReturnError()
    {
        var product = new ProductService.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "Test Product",
            Description = "This is a test product",
            Price = 0,
            ImageUrl = "http://example.com/image.jpg"
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(product));

        Assert.That(ex!.Errors.Any(e => e.PropertyName == "Price"), Is.True);
    }

    [Test]
    // Test if all fields are empty
    // Check should return multiple errors
    public void ProductService_AllFieldsEmpty_ReturnErrors()
    {
        var product = new ProductService.CreateProductCommand
        {
            ProductId = Guid.NewGuid(),
            Title = "",
            Description = "",
            Price = 0,
            ImageUrl = ""
        };

        var ex = Assert.ThrowsAsync<ValidationException>(async () =>
            await _mediator.Send(product));

        Assert.That(ex!.Errors.Count(), Is.EqualTo(4));
    }
}
using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure;
using CampusEatsv2.Infrastructure.Services.CustomerServices;
using CampusEatsv2.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CampusEatsv2.Tests;

[TestFixture]
public class GetStoreMenuQueryTests
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

    public async Task CreateProductsAsync()
    {
        var products = new List<Product>
        {
            new Product { ProductId = Guid.NewGuid(), Title = "Burger", Description = "Tasty burger", Price = 5.99m, ImageUrl = "burger.jpg" },
            new Product { ProductId = Guid.NewGuid(), Title = "Pizza", Description = "Delicious pizza", Price = 8.99m, ImageUrl = "pizza.jpg" }
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }

    [Test]
    public async Task GetStoreMenu_ReturnsAllProducts()
    {
        // Arrange
        await CreateProductsAsync();

        // Act
        var result = await _mediator.Send(new GetStore.GetStoreMenuQuery());

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(p => p.Title == "Burger"));
        Assert.IsTrue(result.Any(p => p.Title == "Pizza"));
    }

    [Test]
    public async Task GetStoreMenu_ReturnsEmptyList_WhenNoProducts()
    {
        // Act
        var result = await _mediator.Send(new GetStore.GetStoreMenuQuery());

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetStoreMenu_ReturnsCorrectProductDetails()
    {
        // Arrange
        var product = new Product { ProductId = Guid.NewGuid(), Title = "Sushi", Description = "Fresh sushi", Price = 12.99m, ImageUrl = "sushi.jpg" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mediator.Send(new GetStore.GetStoreMenuQuery());

        // Assert
        var sushi = result.FirstOrDefault(p => p.Title == "Sushi");
        Assert.IsNotNull(sushi);
        Assert.AreEqual(product.Description, sushi!.Description);
        Assert.AreEqual(product.Price, sushi.Price);
        Assert.AreEqual(product.ImageUrl, sushi.ImageUrl);
    }

    [Test]
    public async Task GetStoreMenu_CancellationTokenWorks()
    {
        // Arrange
        await CreateProductsAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _mediator.Send(new GetStore.GetStoreMenuQuery(), cts.Token)
        );
    }

    // Cool extra tests just for fun! NO AI MADE BY WILLIAM HAHAHAA
    // This is a easter egg
    [Test]
    public async Task GetStoreMenu_PerformanceTest()
    {
        // Arrange
        for (int i = 0; i < 1000; i++)
        {
            _context.Products.Add(new Product { ProductId = Guid.NewGuid(), Title = $"Product {i}", Description = "Test product", Price = 1.99m, ImageUrl = "test.jpg" });
        }
        await _context.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _mediator.Send(new GetStore.GetStoreMenuQuery());
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(1000, result.Count);
        Assert.Less(stopwatch.ElapsedMilliseconds, 500); // Ensure it runs within a reasonable time
    }

}
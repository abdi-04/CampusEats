using CampusEatsv2.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusEatsv2.Infrastructure.Services.Seeding;

public class SeedTestData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<SeedTestData>>();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        // Prevent duplicate seeding
        if (dbContext.Products.Any() || dbContext.Customers.Any() || dbContext.Couriers.Any())
        {
            logger.LogInformation("Test data already exists. Skipping seeding.");
            return;
        }

        var passwordHasher = new PasswordHasher<object>();
        var random = new Random();

        // Products
        var products = new List<Product>
        {
            new()
            {
                ProductId = Guid.NewGuid(),
                Title = "Cheeseburger",
                Price = 89.99m,
                DeliveryFee = 15.00m,
                ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349",
                Description = "Juicy grilled beef burger with cheese, lettuce, and tomato."
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                Title = "Pepperoni Pizza",
                Price = 129.50m,
                DeliveryFee = 20.00m,
                ImageUrl = "https://images.unsplash.com/photo-1601924582975-7e6f1d7b5f9c",
                Description = "Classic pizza topped with pepperoni and mozzarella."
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                Title = "Chicken Wrap",
                Price = 75.00m,
                DeliveryFee = 10.00m,
                ImageUrl = "https://images.unsplash.com/photo-1604908176997-125f25cc6f3d",
                Description = "Grilled chicken wrap with fresh veggies and sauce."
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                Title = "Caesar Salad",
                Price = 65.00m,
                DeliveryFee = 8.00m,
                ImageUrl = "https://images.unsplash.com/photo-1550304943-4f24f54ddde9",
                Description = "Crisp romaine lettuce with Caesar dressing and croutons."
            }
        };

        await dbContext.Products.AddRangeAsync(products);

        // Customers
        var customers = new List<Customer>
        {
            new()
            {
                CustomerId = Guid.NewGuid(),
                FullName = "John Doe",
                Email = "john@example.com",
                PasswordHash = passwordHasher.HashPassword(null!, "password123")
            },
            new()
            {
                CustomerId = Guid.NewGuid(),
                FullName = "Jane Smith",
                Email = "jane@example.com",
                PasswordHash = passwordHasher.HashPassword(null!, "password123")
            }
        };

        await dbContext.Customers.AddRangeAsync(customers);

        // Couriers
        var couriers = new List<Courier>
        {
            new()
            {
                CourierId = Guid.NewGuid(),
                FullName = "Mike Rider",
                Email = "mike@courier.com",
                PasswordHash = passwordHasher.HashPassword(null!, "password123"),
                Status = CourierStatus.Approved,
                CreatedAt = DateTime.UtcNow
            }
        };

        await dbContext.Couriers.AddRangeAsync(couriers);

        var jane = customers.Single(c => c.Email == "jane@example.com");
        var mike = couriers.Single(c => c.Email == "mike@courier.com");

        // Helper:
        // - Create Item
        // - Create Payment
        List<OrderItem> CreateRandomOrderItems()
        {
            var product = products[random.Next(products.Count)];
            var qty = random.Next(1, 3);

            return new()
            {
                new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    ProductId = product.ProductId,
                    Price = product.Price,
                    Quantity = qty
                }
            };
        }

        Payment CreatePayment(Guid orderId, IEnumerable<OrderItem> items, decimal deliveryFee, decimal tip)
            => new()
            {
                PaymentId = Guid.NewGuid(),
                OrderId = orderId,
                Method = PaymentMethods.Vipps,
                Status = PaymentStatus.Paid,
                Amount = items.Sum(i => i.Price * i.Quantity) + deliveryFee + tip
            };

        var orders = new List<Order>();
        var utcYear = DateTime.UtcNow.Year;

        
        // Delivered orders [march]
        
        for (int i = 0; i < 4; i++)
        {
            var items = CreateRandomOrderItems();
            var deliveryFee = random.Next(10, 21);
            var tip = random.Next(8, 18);
            var created = new DateTime(
                utcYear,
                3,
                random.Next(1, 28),
                random.Next(11, 20),
                0,
                0,
                DateTimeKind.Utc);

            var orderId = Guid.NewGuid();

            orders.Add(new Order
            {
                OrderId = orderId,
                Status = OrderStatus.Delivered,
                CustomerId = jane.CustomerId,
                CourierId = mike.CourierId,
                CreationTime = created,
                DeliveryTime = created.AddMinutes(35),
                PickupAddress = "Campus Eats",
                DeliveryFee = deliveryFee,
                Tip = tip,
                OrderItems = items,
                Payment = CreatePayment(orderId, items, deliveryFee, tip)
            });
        }

        // Delivered orders [april]

        for (int i = 0; i < 6; i++)
        {
            var items = CreateRandomOrderItems();
            var deliveryFee = random.Next(10, 21);
            var tip = random.Next(10, 25);
            var created = new DateTime(
                utcYear,
                4,
                random.Next(1, 25),
                random.Next(12, 21),
                0,
                0,
                DateTimeKind.Utc);

            var orderId = Guid.NewGuid();

            orders.Add(new Order
            {
                OrderId = orderId,
                Status = OrderStatus.Delivered,
                CustomerId = jane.CustomerId,
                CourierId = mike.CourierId,
                CreationTime = created,
                DeliveryTime = created.AddMinutes(40),
                PickupAddress = "Campus Eats",
                DeliveryFee = deliveryFee,
                Tip = tip,
                OrderItems = items,
                Payment = CreatePayment(orderId, items, deliveryFee, tip)
            });
        }

        // Cancelled orders

        orders.Add(new Order
        {
            OrderId = Guid.NewGuid(),
            Status = OrderStatus.Cancelled,
            CustomerId = jane.CustomerId,
            CourierId = mike.CourierId,
            CreationTime = DateTime.UtcNow.AddDays(-3),
            PickupAddress = "Dormitory Block A",
            DeliveryFee = 12,
            Tip = 0,
            OrderItems = CreateRandomOrderItems()
        });

        await dbContext.Orders.AddRangeAsync(orders);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Enhanced test data seeded successfully (UTC-safe).");
    }
}

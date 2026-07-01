namespace CampusEatsv2.Core.Models;

/// <summary>
// Customer has his own cart and can add items from the shop
// When ready, they can choose payment method, and proceeed to create order by paying for their Cart
//

// What customer should do?
// Registering
// View and search Store menu
// Open(view) Cart 
// Add/remove item from Cart
// Checkout (incudes Payment)
// View payed Orders/Order history
// Cancell Orders
// Add tip
// Apply to be a courier
/// </summary>
public class Customer
{
    public Guid CustomerId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set;}
    public required string PasswordHash { get; set; }
    public string? DeliveryAddress { get; set; }

}


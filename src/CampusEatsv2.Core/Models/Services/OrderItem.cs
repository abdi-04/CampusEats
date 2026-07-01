namespace CampusEatsv2.Core.Models;
// Order consist of orderitems, orderitems are contained in the order
// Cartitem is transfered when it is processed into a order item
// A orderitem has information from product and snapshot data from Cart dbcontext.

public class OrderItem
{
    public Guid OrderItemId { get; set; }

    // FK -> Order
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // FK -> Product
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Snapshot data (important!)
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

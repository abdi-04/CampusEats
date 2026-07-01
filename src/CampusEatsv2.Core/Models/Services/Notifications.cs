using MediatR;

namespace CampusEatsv2.Core.Models;

// Post notification to the customer when courier change order status
// Including Accepted and Picked
public class OrderStatusChangedNotification : INotification
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public OrderStatus NewStatus { get; set; }
}

// Post notification to all 'Approved' couriers who are not delivering the order now 

public class PostOrderNotification : INotification
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
}
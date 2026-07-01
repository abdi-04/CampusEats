namespace CampusEatsv2.Core.Models;
// Order contain all information to view and take delivery
// Created from filling up PaymentRegistation and Cart
// Order has different status and is sustained through the entire application
public class Order
{
	public Guid OrderId { get; set; }
	public OrderStatus Status { get; set; }
	public decimal DeliveryFee { get; set; }
	public decimal Tip { get; set;} = 0;
	public List<OrderItem> OrderItems { get; set; } = new();
    public Payment? Payment { get; set; }
	public Guid CourierId { get; set; }
	public Guid CustomerId { get; set;}
	public DateTime CreationTime { get; set; }
	public DateTime ?DeliveryTime{ get; set; }
	public required string PickupAddress { get; set; }

    public decimal TotalAmount => (OrderItems?.Sum(item => item.Price * item.Quantity) ?? 0m) + DeliveryFee + Tip;
    public decimal CourierEarning => PaymentRules.CalculateCourierEarning(DeliveryFee, Tip, Status);
}

public class OrderDashboardDto
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid CourierId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Tip { get; set; }
    public DateTime CreationTime { get; set; }

    public decimal CourierEarning =>
        PaymentRules.CalculateCourierEarning(DeliveryFee, Tip, Status);
}

public enum OrderStatus
{
	Awaiting, // While payment proceeded
	Submitted, // After payment, waiting for courier to accept
	Accepted, // After courier accepts, waiting for pickup
	PickedUp, // After delivery pickup, waiting for delivery
	Delivered, // After delivery, waiting for customer confirmation
	Cancelled // Can be cancelled by customer before pickup
}

namespace CampusEatsv2.Core.Models;
/// <summary>
/// Courier model to pick orders and get money for them
/// - Accept/pickup orders
/// - View available orders and order history
/// - View earnings reports
/// </summary>

public class Courier
{
    public Guid CourierId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }

    // From the spec: courier registration must be approved by admin
    public CourierStatus Status { get; set; } = CourierStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation — a courier can deliver many orders
    //public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public enum CourierStatus
{
    Pending,    // registered, waiting for admin approval
    Approved,   // can pick up orders
    Declined    // rejected by admin
}
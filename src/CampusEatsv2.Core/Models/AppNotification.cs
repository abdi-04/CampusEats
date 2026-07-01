namespace CampusEatsv2.Core.Models;
// Model to create Notifications
// Includes logic for notificaitons only about Ordes
// Can be read (Change IsRead value)

public class AppNotification
{
    public Guid NotificationId { get; set; } = Guid.NewGuid();
    public Guid RecipientId { get; set; }
    public Guid OrderId { get; set; }
    public string RecipientLabel { get; set; } = "";
    public OrderStatus Status { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
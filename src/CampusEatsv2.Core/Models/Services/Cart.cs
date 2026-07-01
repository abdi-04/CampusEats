namespace CampusEatsv2.Core.Models;
// Cart, one per customer
// Every customer has one cart where they can look through added items, remove or add more of them
// After creating Order, empty all CartItems

public class Cart
{
   public Guid CartId { get; set; } 
   public Guid UserId {get; set;}
   // A Cart doesn't hold a single ProductId
    // ProductId belongs on CartItem, not Cart
   public List<CartItem> Items {get; set;} = new List<CartItem>();

   public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
   public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  }
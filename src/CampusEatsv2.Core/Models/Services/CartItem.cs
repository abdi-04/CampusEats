namespace CampusEatsv2.Core.Models;
// Items that are visible in customer Cart and Order
// Contain short-view of items that are planned to be ordered/ordered.

public class CartItem
{
	public Guid CartItemId { get; set; } // Primary key for CartItem 
	public Guid CartId { get; set; } // Foreign key to Cart
	public Guid ProductId { get; set;} // Foreign key to Product - used to get price and title for CartItem
	public int Quantity { get; set; } // Quantity of the product in the cart
	public decimal Price { get; set; } // Price of the product in the cart 

	// Navigation properties so Entity Framework Core can do .Include()
    public Cart? Cart { get; set; }
    public Product? Product { get; set; }
}


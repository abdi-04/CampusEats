namespace CampusEatsv2.Core.Models;

// Product desribes all items we have in store and only store.
// It's data used to get price for CartItem which is then transferred to OrderItem
// Product is more of a visual thing and needs to be transferred into Cartitem when pressed
public class Product
{
	public Guid ProductId { get; set; }
	public required string Title { get; set; }
	public decimal Price { get; set; }
	public decimal DeliveryFee { get; set; }
	public required string ImageUrl { get; set; }
	public required string Description { get; set; }
	
}
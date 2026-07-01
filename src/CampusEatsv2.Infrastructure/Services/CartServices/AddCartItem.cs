using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CartServices;

// Take ProductId as a preset to create CartItem
// Add new CartItem to Cart, check if CartItem already exist in Cart

public class AddCartItem
{
    public class AddCartItemCommand : IRequest<Cart>
    {
        public Guid CustomerId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }

    }
    private class Validator : AbstractValidator<AddCartItemCommand>
    {
        public Validator()
        {
            RuleFor(valid => valid.ProductId).
                NotEmpty().WithMessage("No ProductID");
            RuleFor(valid => valid.CustomerId).
                NotEmpty().WithMessage("No CustomerID");
            RuleFor(valid => valid.Quantity).
                GreaterThan(0).WithMessage("Amount should be greater than 0");

        }
    }
    public class AddCartItemHandler : IRequestHandler<AddCartItemCommand, Cart>
    {
        private readonly AppDbContext _context;
        public AddCartItemHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Cart> Handle(AddCartItemCommand request, CancellationToken ct)
        {
            var validator = new Validator();
            validator.ValidateAndThrow(request);

            var product = await _context.Products
                .FirstOrDefaultAsync(prod => prod.ProductId == request.ProductId, ct)
                ?? throw new KeyNotFoundException("Product not found");

            // Create the customer's first cart lazily so menu actions work
            // even if no cart has been provisioned ahead of time.
            var cart = await _context.Carts
                .FirstOrDefaultAsync(existingCart => existingCart.UserId == request.CustomerId, ct);

            if (cart is null)
            {
                cart = new Cart
                {
                    CartId = Guid.NewGuid(),
                    UserId = request.CustomerId,
                    Items = new List<CartItem>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(cart);
            }

            var activeItems = await _context.CartItems
                .Where(item => item .CartId == cart.CartId)
                .ToListAsync(ct);

            var existingItem = activeItems
                .FirstOrDefault(item => item.ProductId == request.ProductId);

            if (existingItem == null)
            {
                var newItem = new CartItem
                {
                    CartItemId = Guid.NewGuid(),
                    CartId = cart.CartId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    Price = product.Price   
                };
                _context.CartItems.Add(newItem);
            }
            else
            {
                // Product in cart already, increase CartItem quantity
                existingItem.Quantity = existingItem.Quantity + request.Quantity;
            }

            await _context.SaveChangesAsync(ct);
            return cart;
            
        }
    }
}

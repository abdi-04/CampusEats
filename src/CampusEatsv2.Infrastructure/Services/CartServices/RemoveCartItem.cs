using System.Data;
using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CartServices;

// Remove CartItem from Cart
// Check if there is only 1 item left, then delete CartItem

public class RemoveCartItem
{
    public class RemoveCartItemCommand : IRequest<Cart>
    {
        public Guid CustomerId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }

    }
    private class Validator : AbstractValidator<RemoveCartItemCommand>
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
    public class RemoveCartItemHandler : IRequestHandler<RemoveCartItemCommand, Cart>
    {
        private readonly AppDbContext _context;
        public RemoveCartItemHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Cart> Handle(RemoveCartItemCommand request, CancellationToken ct)
        {
            var validator = new Validator();
            validator.ValidateAndThrow(request);

            // Find item in DB, check if item quantity will be 0
            // If equal 0, remove CartItem entity from Cart

            var cart = await _context.Carts
                .FirstOrDefaultAsync(cart => cart.UserId == request.CustomerId, ct)
                ?? throw new KeyNotFoundException("Cart not found for customer");

            var activeItems = await _context.CartItems
                .Where(item => item .CartId == cart.CartId)
                .ToListAsync(ct);

            var existingItem = activeItems
                .FirstOrDefault(item => item.ProductId == request.ProductId)
                ?? throw new KeyNotFoundException("Item not found in cart");                

            if (existingItem.Quantity < request.Quantity)
            {
                throw new ArgumentOutOfRangeException("Removed quantity could not be greater than current item quantity");
            }
            //Remove items
            existingItem.Quantity = existingItem.Quantity - request.Quantity;

            if (existingItem.Quantity == 0) 
            { 
                _context.CartItems.Remove(existingItem);
            }

            await _context.SaveChangesAsync(ct);
            return cart;
        }
    }
}

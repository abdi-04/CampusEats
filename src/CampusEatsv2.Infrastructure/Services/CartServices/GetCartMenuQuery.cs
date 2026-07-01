using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CartServices;
//Shows CartItems in the given customer directory

public class ShowCustomerCart
{
    public class ShowCustomerCartQuery : IRequest<Cart>
    {
        public Guid CustomerId { get; set; }
    }
    private class Validator : AbstractValidator<ShowCustomerCartQuery>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("CustomerId cannot be null");
        }
    }
    public class ShowCustomerCartHandler : IRequestHandler<ShowCustomerCartQuery, Cart>
    {
        private readonly AppDbContext _context;
        public ShowCustomerCartHandler(AppDbContext context) 
        {
            _context = context;
        }

        public async Task<Cart> Handle(ShowCustomerCartQuery request, CancellationToken cancellationToken)
        {
            var validator = new Validator();
            validator.ValidateAndThrow(request);

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == request.CustomerId, cancellationToken);

            if (cart is null)
            {
                return new Cart
                {
                    CartId = Guid.NewGuid(),
                    UserId = request.CustomerId,
                    Items = new List<CartItem>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            cart.Items = await _context.CartItems
                .Include(item => item.Product)
                .Where(item => item .CartId == cart.CartId)
                .ToListAsync(cancellationToken);

            return cart;
        }
    }
}

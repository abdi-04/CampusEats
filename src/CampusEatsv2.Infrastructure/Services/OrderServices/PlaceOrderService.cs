using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.OrderServices;

//Checkout Order
public class PlaceOrderService
{
    public class PlaceOrderCommand : IRequest<Order>
    {
        public Guid CustomerId { get; set; }
        public Guid OrderId { get; set; }
        public decimal DeliveryFee { get; set; }
        public PaymentMethods Payment { get; set; }

        // New time and related fields
        public DateTime CreationTime { get; set; }
        public required string PickupAddress { get; set; }
    }

    private class Validator : AbstractValidator<PlaceOrderCommand>
    {
        public Validator()
        {
            // Basic validation rules for placing an order no empty ID
            RuleFor(x => x.OrderId).NotEmpty().NotNull();
            RuleFor(x => x.CustomerId).NotEmpty().NotNull();

            // Payment method must be a valid enum value
            RuleFor(x => x.Payment)
                .IsInEnum()
                .WithMessage("Invalid payment method");
     
            // Delivery fee math must math correctly!
            RuleFor(x => x.DeliveryFee)
                .GreaterThan(0)
                .WithMessage("Delivery fee must be greater than 0");
        }
    }

    public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Order>
    {
        private readonly AppDbContext _context;
        private readonly IMediator _mediator;

        public PlaceOrderHandler(AppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<Order> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
        {
            new Validator().ValidateAndThrow(request);

            // Use provided CreationTime if set, otherwise default to UtcNow
            var creationTime = request.CreationTime == default ? DateTime.UtcNow : request.CreationTime;

            var cart = await _context.Carts
                .FirstOrDefaultAsync(existingCart => existingCart.UserId == request.CustomerId, cancellationToken);

            var activeCartItems = cart is null
                ? new List<CartItem>()
                : await _context.CartItems
                    .Where(item => item.CartId == cart.CartId)
                    .ToListAsync(cancellationToken);

            var orderItems = activeCartItems.Select(ci => new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                ProductId = ci.ProductId,
                Price = ci.Price,
                Quantity = ci.Quantity
            }).ToList();

            if (!orderItems.Any())
            {
                throw new InvalidOperationException("Cannot place an order with an empty cart.");
            }

            var orderDTO = new Order
            {
                OrderId = request.OrderId,
                CustomerId = request.CustomerId,
                DeliveryFee = request.DeliveryFee,
                Tip = 0, // Tip can only be set during delivery
                OrderItems = orderItems,
                Status = OrderStatus.Submitted, // enforce, don't trust caller
                CreationTime = creationTime,
                DeliveryTime = null,
                PickupAddress = request.PickupAddress,
                CourierId = Guid.Empty, // No courier assigned until AcceptOrder
                Payment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    Method = request.Payment,
                    Status = PaymentStatus.Pending,
                    Amount = orderItems.Sum(item => item.Price * item.Quantity) + request.DeliveryFee
                }
            };
            _context.Orders.Add(orderDTO);
            _context.CartItems.RemoveRange(activeCartItems); // Clear cart items after placing order

            await _context.SaveChangesAsync(cancellationToken);
            
            // Adding push notification
            await _mediator.Publish(new PostOrderNotification
            {
                OrderId = orderDTO.OrderId,
                CustomerId = orderDTO.CustomerId
            });

            return orderDTO;
        }
    }
}

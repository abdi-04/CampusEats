using CampusEatsv2.Core.DomainEvents;
using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.OrderServices;

// Confirmation from customer that order is delivered
// Finalizes delivery + tip and publishes receipt event
public class ConfirmDeliveryCustomer
{
    public class ConfirmDeliveryCustomerCommand : IRequest<Order>
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; } // Customer must own the order
        public decimal Tip { get; set; }     // tip
    }

    public class Validator : AbstractValidator<ConfirmDeliveryCustomerCommand>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Order ID is required.");

            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("Customer ID is required.");

            RuleFor(x => x.Tip)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Tip cannot be negative.");
        }
    }

    public class ConfirmDeliveryCustomerHandler
        : IRequestHandler<ConfirmDeliveryCustomerCommand, Order>
    {
        private readonly AppDbContext _context;
        private readonly IMediator _mediator;

        public ConfirmDeliveryCustomerHandler(
            AppDbContext context,
            IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<Order> Handle(
            ConfirmDeliveryCustomerCommand request,
            CancellationToken ct)
        {
            new Validator().ValidateAndThrow(request);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == request.OrderId &&
                    o.CustomerId == request.CustomerId,
                    ct)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.Status != OrderStatus.PickedUp)
            {
                throw new ArgumentException(
                    $"Order must be PickedUp to be Delivered. Current status: {order.Status}");
            }

            //  Finalize order state
            order.Status = OrderStatus.Delivered;
            order.DeliveryTime = DateTime.UtcNow;

            //  Finalize tip BEFORE receipt event
            order.Tip = request.Tip;

            await _context.SaveChangesAsync(ct);

            //  Publish FINAL receipt event using persisted values
            await _mediator.Publish(
                new OrderDeliveredDomainEvent(
                    order.OrderId,
                    order.TotalAmount,   // items + delivery + tip
                    order.DeliveryFee,   // persisted at checkout
                    order.Tip            // finalized here
                ),
                ct);

            return order;
        }
    }
}
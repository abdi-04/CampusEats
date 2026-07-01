using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CourierServices;

// Set "OrderStatus.Approved" and set CourierId
public class AcceptOrder
{
    public class AcceptOrderCommand : IRequest<Order>
    {
        public Guid CourierId { get; set; }
        public Guid OrderId { get; set; }
    }

    private class Validator : AbstractValidator<AcceptOrderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.CourierId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
        }
    }

    public class PickupOrderHandler : IRequestHandler<AcceptOrderCommand, Order>
    {
        private readonly AppDbContext _context;
        private readonly IMediator _mediator;
        public PickupOrderHandler(AppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<Order> Handle(AcceptOrderCommand request, CancellationToken cancellationToken)
        {
            new Validator().ValidateAndThrow(request);

            var courier = await _context.Couriers
                .FirstOrDefaultAsync(c => c.CourierId == request.CourierId, cancellationToken)
                ?? throw new KeyNotFoundException("Courier not found.");

            if (courier.Status != CourierStatus.Approved)
                throw new InvalidOperationException("Courier is not approved to pick up orders.");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.Status != OrderStatus.Submitted)
                throw new InvalidOperationException("Order is not available for acceptance.");

            // Assign courier and update status
            
            order.CourierId = request.CourierId;
            order.Status = OrderStatus.Accepted;

            await _context.SaveChangesAsync(cancellationToken);

            // Adding push notification
            await _mediator.Publish(new OrderStatusChangedNotification
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    NewStatus = order.Status
                });

            return order;

        }
    }
}
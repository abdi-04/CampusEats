using CampusEatsv2.Core.DomainEvents;
using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CourierServices;
// Set OrderStatus.PickedUp
public class PickupOrder
{
    public class PickupOrderCommand : IRequest<Order>
    {
        public Guid CourierId { get; set; }
        public Guid OrderId { get; set; }
    }

    public class Validator : AbstractValidator<PickupOrderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.CourierId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
        }
    }

    public class PickupOrderHandler : IRequestHandler<PickupOrder.PickupOrderCommand, Order>
    {
        private readonly AppDbContext _context;
        private readonly IMediator _mediator;

        public PickupOrderHandler(AppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<Order> Handle(PickupOrder.PickupOrderCommand request, CancellationToken ct)
        {
            new PickupOrder.Validator().ValidateAndThrow(request);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.OrderId == request.OrderId &&
                    o.CourierId == request.CourierId, ct)
                ?? throw new KeyNotFoundException("Order not found or does not belong to this courier.");

            if (order.Status != OrderStatus.Accepted)
                throw new InvalidOperationException("Order must be accepted before marking as picked up.");

            order.Status = OrderStatus.PickedUp;

            await _context.SaveChangesAsync(ct);

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
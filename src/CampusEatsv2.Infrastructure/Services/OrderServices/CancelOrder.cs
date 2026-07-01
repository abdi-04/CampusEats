using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.OrderServices;

// Set 'OrderStatus.Cancelled'
public class CancelOrder
{
    public class CancelOrderCommand : IRequest<Order>
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; } // ensures a user can only cancel their own order
    }

    private class Validator : AbstractValidator<CancelOrderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.CustomerId).NotEmpty();
        }
    }

    public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, Order>
    {
        private readonly AppDbContext _context;
        private readonly IMediator _mediator;

        public CancelOrderHandler(AppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<Order> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            new Validator().ValidateAndThrow(request);

            var orderDTO = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId
                                       && o.CustomerId == request.CustomerId,
                                       cancellationToken);

            if (orderDTO is null)
                throw new KeyNotFoundException($"Order {request.OrderId} not found for this user.");

            // !! Only cancellable if not already delivered, cancelled or picked up
            if (orderDTO.Status == OrderStatus.Delivered || orderDTO.Status == OrderStatus.Cancelled || orderDTO.Status == OrderStatus.PickedUp)
                throw new InvalidOperationException($"Cannot cancel an order with status '{orderDTO.Status}'.");

            orderDTO.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);

            // Adding push notification
            await _mediator.Publish(new OrderStatusChangedNotification
            {
                OrderId = orderDTO.OrderId,
                CustomerId = orderDTO.CustomerId,
                NewStatus = orderDTO.Status
            });

            return orderDTO;
        }
    }
}
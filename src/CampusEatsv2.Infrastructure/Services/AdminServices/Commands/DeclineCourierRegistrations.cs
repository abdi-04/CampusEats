using CampusEatsv2.Core.Models;
using CampusEatsv2.Core.DomainEvents;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.AdminServices;

// Changes status of Courier to 'CourierStatus.Declined'

public class DeclineCourierRegistration
{
    public class DeclineCourierRegistrationCommand : IRequest<Courier>
    {
        public Guid CourierId { get; set; }
    }

    private class Validator : AbstractValidator<DeclineCourierRegistrationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.CourierId).NotEmpty();
        }
    }

    public class DeclineCourierRegistrationHandler : IRequestHandler<DeclineCourierRegistrationCommand, Courier>
    {
        private readonly AppDbContext _context;

        private readonly IMediator _mediator;

        public DeclineCourierRegistrationHandler(AppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<Courier> Handle(DeclineCourierRegistrationCommand request, CancellationToken cancellationToken)
        {
            new Validator().ValidateAndThrow(request);

            var courier = await _context.Couriers
                .FirstOrDefaultAsync(c => c.CourierId == request.CourierId, cancellationToken)
                ?? throw new KeyNotFoundException("Courier not found.");

            if (courier.Status != CourierStatus.Pending)
                throw new InvalidOperationException("Only pending couriers can be declined.");

            courier.Status = CourierStatus.Declined;

            await _context.SaveChangesAsync(cancellationToken);
            // Domain event for email pipeline
            await _mediator.Publish(
                new CourierDeclinedDomainEvent(
                    courier.CourierId,
                    courier.FullName,
                    courier.Email),
                cancellationToken);
            return courier;
        }
    }
}
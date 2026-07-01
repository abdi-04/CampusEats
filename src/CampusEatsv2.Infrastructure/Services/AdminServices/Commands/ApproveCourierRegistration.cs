using CampusEatsv2.Core.Models;
using CampusEatsv2.Core.DomainEvents;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.AdminServices;

// Changes status of Courier to 'CourierStatus.Approved'
public class ApproveCourierRegistration
{
    public class ApproveCourierRegistrationCommand : IRequest<Courier>
    {
        public Guid CourierId { get; set; }
    }

    private class Validator : AbstractValidator<ApproveCourierRegistrationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.CourierId).NotEmpty();
        }
    }

    public class ApproveCourierRegistrationHandler : IRequestHandler<ApproveCourierRegistrationCommand, Courier>
    {
        private readonly AppDbContext _context;

        private readonly IMediator _mediator;

        public ApproveCourierRegistrationHandler(AppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<Courier> Handle(ApproveCourierRegistrationCommand request, CancellationToken ct)
        {
            new Validator().ValidateAndThrow(request);

            var courier = await _context.Couriers
                .FirstOrDefaultAsync(c => c.CourierId == request.CourierId, ct)
                ?? throw new KeyNotFoundException("Courier not found.");

            if (courier.Status != CourierStatus.Pending)
                throw new InvalidOperationException("Only pending couriers can be approved.");

            courier.Status = CourierStatus.Approved;

            await _context.SaveChangesAsync(ct);
            // Domain event for email pipeline
            await _mediator.Publish(
                new CourierApprovedDomainEvent(
                    courier.CourierId,
                    courier.FullName,
                    courier.Email),
                ct);
            return courier;
        }
    }
}
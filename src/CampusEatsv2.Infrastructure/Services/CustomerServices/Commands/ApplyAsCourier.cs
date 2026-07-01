using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CustomerServices;

public class ApplyAsCourier
{
    public class ApplyAsCourierCommand : IRequest<Courier>
    {
        public Guid CustomerId { get; set; }
    }

    private class Validator : AbstractValidator<ApplyAsCourierCommand>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
        }
    }

    public class ApplyAsCourierHandler : IRequestHandler<ApplyAsCourierCommand, Courier>
    {
        private readonly AppDbContext _context;

        public ApplyAsCourierHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Courier> Handle(ApplyAsCourierCommand request, CancellationToken cancellationToken)
        {
            new Validator().ValidateAndThrow(request);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken)
                ?? throw new KeyNotFoundException("Customer not found.");

            // Prevent duplicate applications
            bool alreadyApplied = await _context.Couriers
                .AnyAsync(c => c.Email == customer.Email, cancellationToken);

            if (alreadyApplied)
                throw new InvalidOperationException("You have already submitted a courier application.");

            var courier = new Courier
            {
                CourierId = Guid.NewGuid(),
                FullName = customer.FullName,
                Email = customer.Email,
                PasswordHash = customer.PasswordHash, // reuse credentials — same person
                Status = CourierStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Couriers.Add(courier);
            await _context.SaveChangesAsync(cancellationToken);
            return courier;
        }
    }
}
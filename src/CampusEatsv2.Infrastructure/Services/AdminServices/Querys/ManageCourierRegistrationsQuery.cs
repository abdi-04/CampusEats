using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.AdminServices;
// Returns Couriers  with "CourierStatus.Pending"
public class ManageCourierRegistrations
{
    public class ManageCourierRegistrationsQuery : IRequest<List<Courier>>
    {
        public string? SearchEmail { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    private class Validator : AbstractValidator<ManageCourierRegistrationsQuery>
    {
        public Validator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(50)
                .WithMessage("PageSize cannot exceed 50.");

            RuleFor(x => x.SearchEmail)
                .MaximumLength(200)
                .WithMessage("SearchEmail is too long.")
                .When(x => !string.IsNullOrEmpty(x.SearchEmail));

            RuleFor(x => x.SearchEmail)
                .EmailAddress()
                .WithMessage("Invalid email format.")
                .When(x => !string.IsNullOrEmpty(x.SearchEmail));
        }
    }

    public class ManageCourierRegistrationsHandler : IRequestHandler<ManageCourierRegistrationsQuery, List<Courier>>
    {
        private readonly AppDbContext _context;

        public ManageCourierRegistrationsHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Courier>> Handle(ManageCourierRegistrationsQuery request, CancellationToken cancellationToken)
        {
            new Validator().ValidateAndThrow(request);

            var query = _context.Couriers
                .Where(c => c.Status == CourierStatus.Pending);

            if (!string.IsNullOrEmpty(request.SearchEmail))
            {
                query = query.Where(c => c.Email.Contains(request.SearchEmail));
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
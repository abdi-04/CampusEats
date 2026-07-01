using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.AdminServices;

public class InviteAdminUser
{
    public enum SourceType
    {
        Customer,
        Courier
    }

    public class AdminInviteCandidate
    {
        public Guid SourceId { get; set; }
        public string Email { get; set; } = string.Empty;
        public SourceType SourceType { get; set; }
        public string? CourierStatus { get; set; }
    }

    public class SearchEligibleAdminUsersQuery : IRequest<List<AdminInviteCandidate>>
    {
        public SourceType SourceType { get; set; }
        public string? SearchEmail { get; set; }
    }

    private class SearchEligibleAdminUsersValidator : AbstractValidator<SearchEligibleAdminUsersQuery>
    {
        public SearchEligibleAdminUsersValidator()
        {
            RuleFor(x => x.SearchEmail)
                .MaximumLength(200)
                .WithMessage("Search email cannot be longer than 200 characters.");
        }
    }

    public class SearchEligibleAdminUsersHandler : IRequestHandler<SearchEligibleAdminUsersQuery, List<AdminInviteCandidate>>
    {
        private readonly AppDbContext _context;

        public SearchEligibleAdminUsersHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AdminInviteCandidate>> Handle(SearchEligibleAdminUsersQuery request, CancellationToken cancellationToken)
        {
            var validator = new SearchEligibleAdminUsersValidator();
            validator.ValidateAndThrow(request);

            var normalizedSearch = request.SearchEmail?.Trim().ToLower();

            var existingAdminEmails = await _context.Admins
                .AsNoTracking()
                .Select(a => a.Email.ToLower())
                .ToListAsync(cancellationToken);

            if (request.SourceType == SourceType.Customer)
            {
                var query = _context.Customers
                    .AsNoTracking()
                    .Where(c => !existingAdminEmails.Contains(c.Email.ToLower()));

                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    query = query.Where(c => c.Email.ToLower().Contains(normalizedSearch));
                }

                return await query
                    .OrderBy(c => c.Email)
                    .Select(c => new AdminInviteCandidate
                    {
                        SourceId = c.CustomerId,
                        Email = c.Email,
                        SourceType = SourceType.Customer,
                        CourierStatus = null
                    })
                    .ToListAsync(cancellationToken);
            }
            else
            {
                var query = _context.Couriers
                    .AsNoTracking()
                    .Where(c => !existingAdminEmails.Contains(c.Email.ToLower()));

                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    query = query.Where(c => c.Email.ToLower().Contains(normalizedSearch));
                }

                return await query
                    .OrderBy(c => c.Email)
                    .Select(c => new AdminInviteCandidate
                    {
                        SourceId = c.CourierId,
                        Email = c.Email,
                        SourceType = SourceType.Courier,
                        CourierStatus = c.Status.ToString()
                    })
                    .ToListAsync(cancellationToken);
            }
        }
    }

    public class InviteAdminUserCommand : IRequest<Admin>
    {
        public SourceType SourceType { get; set; }
        public Guid SourceId { get; set; }
        public Guid? InvitedByAdminId { get; set; }
        public string InitialPassword { get; set; } = string.Empty;
    }

    private class Validator : AbstractValidator<InviteAdminUserCommand>
    {
        public Validator()
        {
            RuleFor(x => x.SourceId)
                .NotEmpty()
                .WithMessage("SourceId is required.");

            RuleFor(x => x.InitialPassword)
                .NotEmpty()
                .WithMessage("Initial password is required.")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters.")
                .MaximumLength(32)
                .WithMessage("Password must be 32 characters or less.");
        }
    }

    public class InviteAdminUserHandler : IRequestHandler<InviteAdminUserCommand, Admin>
    {
        private readonly AppDbContext _context;

        public InviteAdminUserHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Admin> Handle(InviteAdminUserCommand request, CancellationToken cancellationToken)
        {
            var validator = new Validator();
            validator.ValidateAndThrow(request);

            string email;

            switch (request.SourceType)
            {
                case SourceType.Customer:
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.CustomerId == request.SourceId, cancellationToken);

                    if (customer is null)
                        throw new KeyNotFoundException("Customer not found.");

                    email = customer.Email;
                    break;

                case SourceType.Courier:
                    var courier = await _context.Couriers
                        .FirstOrDefaultAsync(c => c.CourierId == request.SourceId, cancellationToken);

                    if (courier is null)
                        throw new KeyNotFoundException("Courier not found.");

                    email = courier.Email;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.SourceType), "Unknown source type.");
            }

            var alreadyAdmin = await _context.Admins
                .AsNoTracking()
                .AnyAsync(a => a.Email == email, cancellationToken);

            if (alreadyAdmin)
                throw new InvalidOperationException("A user with this email is already an admin.");

            var hasher = new PasswordHasher<Admin>();

            var admin = new Admin
            {
                AdminId = Guid.NewGuid(),
                Email = email,
                PasswordHash = hasher.HashPassword(new Admin { Email = email, PasswordHash = string.Empty }, request.InitialPassword),
                IsFirstLogin = true,
                InvitedByAdminId = request.InvitedByAdminId
            };

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync(cancellationToken);

            return admin;
        }
    }
}
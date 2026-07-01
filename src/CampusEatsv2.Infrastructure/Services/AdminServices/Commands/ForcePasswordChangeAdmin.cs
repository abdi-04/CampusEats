using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CampusEatsv2.Infrastructure.Services.AdminServices;


// Only allowed for the admins and only first login
// Changes password

public class ForcePasswordChangeAdmin
{
    public record ForcePasswordChangeCommand : IRequest<Admin>
    {
        public Guid AdminId { get; init; }
        public required string NewPassword { get; init; } // No ComfirmPassword, check it inside frontend
    }

    public class Validator : AbstractValidator<ForcePasswordChangeCommand>
    {
        public Validator()
        {
            RuleFor(x => x.AdminId).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(32);
        }
    }

    public class Handler : IRequestHandler<ForcePasswordChangeCommand, Admin>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Admin> Handle(ForcePasswordChangeCommand request, CancellationToken cancellationToken)
        {
            new Validator().ValidateAndThrow(request);

            var admin = await _context.Admins.FindAsync(request.AdminId, cancellationToken);
            if (admin == null)
            {
                throw new KeyNotFoundException("Admin not found");
            }

            if (!admin.IsFirstLogin)
            {
                throw new InvalidOperationException("Password change is only allowed during first login");
            }

            var hasher = new PasswordHasher<Admin>();
            admin.PasswordHash = hasher.HashPassword(admin, request.NewPassword);
            admin.IsFirstLogin = false;

            await _context.SaveChangesAsync(cancellationToken);
            return admin;
        }
    }
}
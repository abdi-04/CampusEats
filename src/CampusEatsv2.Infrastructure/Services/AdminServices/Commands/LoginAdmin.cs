using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace CampusEatsv2.Infrastructure.Services.AdminServices;

public class LoginAdmin
{
    public class LoginAdminCommand : IRequest<LoginAdminResult>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Return this instead of raw Admin so the caller knows
    // whether a password change is required
    public class LoginAdminResult
    {
        public Admin Admin { get; set; } = null!;
        public bool RequiresPasswordChange { get; set; }
    }

    public class Validator : AbstractValidator<LoginAdminCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }

    public class LoginAdminHandler : IRequestHandler<LoginAdminCommand, LoginAdminResult>
    {
        private readonly AppDbContext _context;

        public LoginAdminHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LoginAdminResult> Handle(LoginAdminCommand request, CancellationToken cancellationToken)
        {

            // Find admin by email
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken)
                ?? throw new UnauthorizedAccessException("Invalid email or password.");

            // Check if password correct
            var hasher = new PasswordHasher<Admin>();
            var result = hasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid email or password.");

            // Return admin and flag if password change is needed
            // If RequiresPasswordChange is true, get redirected
            return new LoginAdminResult
            {
                Admin = admin,
                RequiresPasswordChange = admin.IsFirstLogin
            };
        }
    }
}
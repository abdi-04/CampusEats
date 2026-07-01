using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace CampusEatsv2.Infrastructure.Services.CourierServices;


public class LoginCourier
{
    public class LoginCourierCommand : IRequest<Courier>
    {
        public required string Email { get; set; }
        public required string Password { get; set; } 
        public CourierStatus? Status { get; set; }
    }
    public class Validator : AbstractValidator<LoginCourierCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.");
        }
    }
    public class LoginCourierHandler : IRequestHandler<LoginCourierCommand, Courier>
    {
        private readonly AppDbContext _context;
        public LoginCourierHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Courier> Handle(LoginCourierCommand request, CancellationToken cancellationToken)
        {
            var courier = await _context.Couriers
                .FirstOrDefaultAsync(c => c.Email == request.Email, cancellationToken)
                ?? throw new UnauthorizedAccessException("Invalid email or password.");

            var hasher = new PasswordHasher<Courier>();
            var result = hasher.VerifyHashedPassword(courier, courier.PasswordHash!, request.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid email or password.");
            
            if (courier.Status != CourierStatus.Approved)
                throw new UnauthorizedAccessException("Courier registration is not approved yet.");
            
            return courier;
                
            
        }
    }


}

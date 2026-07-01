using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace CampusEatsv2.Infrastructure.Services.CustomerServices;


public class LoginCustomer
{
    public class LoginCustomerCommand : IRequest<Customer>
    {
        public required string Email { get; set; }
        public required string Password { get; set; } 
    }
    public class Validator : AbstractValidator<LoginCustomerCommand>
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
    public class LoginCustomerHandler : IRequestHandler<LoginCustomerCommand, Customer>
    {
        private readonly AppDbContext _context;
        public LoginCustomerHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Customer> Handle(LoginCustomerCommand request, CancellationToken cancellationToken)
        {
            //Find customer by email
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == request.Email, cancellationToken)
                ?? throw new UnauthorizedAccessException("Invalid email or password.");

            //Verify password
            var hasher = new PasswordHasher<Customer>();
            var result = hasher.VerifyHashedPassword(customer, customer.PasswordHash!, request.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid email or password.");

            // Return the customer
            return customer;
                
            
        }
    }


}

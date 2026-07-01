using CampusEatsv2.Core.Models;
using CampusEatsv2.Core.DomainEvents;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CustomerServices;

public class RegisterCustomer
{
    public class RegisterCustomerCommand : IRequest<Customer>
    {
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? Address { get; set; }
    }

    public class Validator : AbstractValidator<RegisterCustomerCommand>
    {
        public Validator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty();

            RuleFor(x => x.Password)
                .NotEmpty()
                .Length(8, 32)
                .WithMessage("The password is under 8 symbols or over 32 symbols");

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Invalid Email address");
        }
    }

    public class RegisterCustomerHandler
    : IRequestHandler<RegisterCustomerCommand, Customer>
    {
        private readonly AppDbContext _context;
        private readonly IMediator _mediator;
    
        public RegisterCustomerHandler(AppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }
    
        public async Task<Customer> Handle(
            RegisterCustomerCommand request,
            CancellationToken ct)
        {
            var isEmailUsed = await _context.Customers
                .AnyAsync(c => c.Email == request.Email, ct);
    
            if (isEmailUsed)
                throw new InvalidOperationException("Email is already in use");
    
            var customer = new Customer
            {
                CustomerId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = string.Empty, 
                DeliveryAddress = request.Address
            };
    
            var hasher = new PasswordHasher<Customer>();
            customer.PasswordHash =
                hasher.HashPassword(customer, request.Password);
    
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync(ct);
    
            // Domain event for email pipeline
            await _mediator.Publish(
                new CustomerRegisteredDomainEvent(
                    customer.CustomerId,
                    customer.Email,
                    customer.FullName),
                ct);
    
            return customer;
        }
    }
}
using CampusEatsv2.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CustomerServices;

public class LoginOrRegisterGoogleCustomer
{
    public class Command : IRequest<Customer>
    {
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class Handler : IRequestHandler<Command, Customer>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Customer> Handle(Command request, CancellationToken ct)
        {
            // If they've logged in before, just return their existing record
            var existing = await _db.Customers
                .FirstOrDefaultAsync(c => c.Email == request.Email, ct);

            if (existing != null)
                return existing;

            // First time — create Customer from Google credentials
            // Sentinel hash means this account can never be used for password login
            var customer = new Customer
            {
                FullName = request.Name,
                Email = request.Email,
                PasswordHash = "GOOGLE_OAUTH"
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(ct);
            return customer;
        }
    }
}
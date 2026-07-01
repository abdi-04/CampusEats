using CampusEatsv2.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CourierServices;

// Represents a use case: Log in as a courier using Google account 
public class LoginGoogleAsCourier
{
    public class Command : IRequest<Courier> // A Command (input)
    {
        // <summary>
        // Gets or sets the email received from Google login.
        // This email is used to:
        // 1. Check if a courier application exists with this email.
        // 2. Determine the courier's status (Pending, Approved, Declined).
        public string Email { get; set; } = "";
    }

    // <summary>
    // This class handles the Command.
    // It:Takes a Command
    // Returns a Courier
    public class Handler : IRequestHandler<Command, Courier> // A Handler (business logic)
    {
        private readonly AppDbContext _db; // Stores a reference to the database context.

        // <summary>
        // Uses constructor injection.
        // AppDbContext is provided automatically by ASP.NET Core’s dependency injection.
        // Stores it in _db for later database access.
        public Handler(AppDbContext db) => _db = db;
        
        // The method runs asynchronously (async), returns a Task<Courier> that will produce a courier in the future 
        // takes a request containing the email from Google 
        // and uses a cancellation token (ct) to allow the operation to be cancelled if needed.
        public async Task<Courier> Handle(Command request, CancellationToken ct)
        {
             // Fetching the courier from the database
            var courier = await _db.Couriers
                .FirstOrDefaultAsync(c => c.Email == request.Email, ct);

            if (courier == null)
                throw new InvalidOperationException("No courier application found.");

            if (courier.Status == CourierStatus.Pending)
                throw new UnauthorizedAccessException(
                    "Your courier application is still pending admin approval.");

            if (courier.Status == CourierStatus.Declined)
                throw new UnauthorizedAccessException(
                    "Your courier application was declined.");

            return courier;
        }
    }
}
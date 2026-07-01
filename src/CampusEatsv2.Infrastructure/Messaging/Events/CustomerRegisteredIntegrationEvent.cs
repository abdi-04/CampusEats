namespace CampusEatsv2.Infrastructure.Messaging.Events;
// Cross service communication bus that notifys when a state changes

public sealed record CustomerRegisteredIntegrationEvent(
    Guid CustomerId,
    string Email,
    string FullName
);
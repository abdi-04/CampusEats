namespace CampusEatsv2.Infrastructure.Messaging.Events;
// Cross service communication bus that notifys when a state changes

public sealed record OrderPickedUpIntegrationEvent(
    Guid OrderId,
    string Email,
    string FullName
);

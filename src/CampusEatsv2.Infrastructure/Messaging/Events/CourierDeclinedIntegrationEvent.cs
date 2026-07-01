namespace CampusEatsv2.Infrastructure.Messaging.Events;
// Cross service communication bus that notifys when a state changes

public sealed record CourierDeclinedIntegrationEvent(
    Guid CourierId,
    string FullName,
    string Email
);

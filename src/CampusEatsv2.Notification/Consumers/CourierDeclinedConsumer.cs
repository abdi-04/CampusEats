using CampusEatsv2.Infrastructure.Messaging.Events;
using CampusEatsv2.Notification.Services;

namespace CampusEatsv2.Notification.Consumers;
// Consumer contains the Email that is sent the smpt4dev
// The only thing is does is consume a event when handle event occurs from RabbitMQ
// This one is for CourierApproved

public sealed class CourierDeclinedConsumer
{
    private readonly SmtpEmailSender _emailSender;

    public CourierDeclinedConsumer(SmtpEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task Handle(CourierDeclinedIntegrationEvent evt)
    {
        var body = $"""
        Hi {evt.FullName},

        Unfortunatly,
        Your courier account has been declined.

        You can still order items as a customer!

        Thank you for applying to CampusEats!
        """;

        await _emailSender.SendAsync(
            evt.Email,
            "You have been declined as a CampusEats courier 🚴",
            body);
    }
}
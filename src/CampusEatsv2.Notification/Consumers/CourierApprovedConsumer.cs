using CampusEatsv2.Infrastructure.Messaging.Events;
using CampusEatsv2.Notification.Services;

namespace CampusEatsv2.Notification.Consumers;
// Consumer contains the Email that is sent the smpt4dev
// The only thing is does is consume a event when handle event occurs from RabbitMQ
// This one is for CourierApproved

public sealed class CourierApprovedConsumer
{
    private readonly SmtpEmailSender _emailSender;

    public CourierApprovedConsumer(SmtpEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task Handle(CourierApprovedIntegrationEvent evt)
    {
        var body = $"""
        Hi {evt.FullName},

        🎉 Congratulations!
        Your courier account has been approved.

        You can now log in and start delivering orders.

        Thank you for joining CampusEats!
        """;

        await _emailSender.SendAsync(
            evt.Email,
            "You are approved as a CampusEats courier 🚴",
            body);
    }
}
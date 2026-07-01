using CampusEatsv2.Infrastructure.Messaging.Events;
using CampusEatsv2.Notification.Services;

namespace CampusEatsv2.Notification.Consumers;
// Consumer contains the Email that is sent the smpt4dev
// The only thing is does is consume a event when handle event occurs from RabbitMQ
// This one is for CustomerRegistered

public sealed class CustomerRegisteredConsumer
{
    private readonly SmtpEmailSender _emailSender;

    public CustomerRegisteredConsumer(SmtpEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task Handle(CustomerRegisteredIntegrationEvent evt)
    {
        var body = $"""
        <p>Hi <strong>{evt.FullName}</strong>,</p>

        <p>Welcome to <strong>CampusEats</strong> 🎉</p>

        <p>You can now order food, track deliveries, and support local couriers.</p>

        <p>
            If you have any questions, contact us at
            <a href="mailto:no.reply.campuseats@gmail.com">
                no.reply.campuseats@gmail.com
            </a>
        </p>

        <p>– The CampusEats Team</p>
        """;

        await _emailSender.SendAsync(
            to: evt.Email,
            subject: "Welcome to CampusEats 🎉",
            body: body);
    }
}
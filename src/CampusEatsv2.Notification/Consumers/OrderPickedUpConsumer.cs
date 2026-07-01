using CampusEatsv2.Core.Models;
using CampusEatsv2.Infrastructure.Messaging.Events;
using CampusEatsv2.Notification.Services;

namespace CampusEatsv2.Notification.Consumers;
// Consumer contains the Email that is sent the smpt4dev
// The only thing is does is consume a event when handle event occurs from RabbitMQ
// This one is for OrderPickedup

public sealed class OrderPickedUpConsumer
{
    private readonly SmtpEmailSender _emailSender;

    public OrderPickedUpConsumer(SmtpEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task Handle(OrderPickedUpIntegrationEvent evt)
    {
        var body = $"""
        <p>Hi <strong>Hi {evt.FullName}</strong>,</p>

        <p>Good news! 🚴</p>

        <p>Your order {evt.OrderId} has been <strong>picked up</strong> and is on the way to you.</p>

        <p>Please be ready to receive your delivery.</p>

        <p>
            Questions? Contact:
            <a href="mailto:no.reply.campuseats@gmail.com">
                no.reply.campuseats@gmail.com
            </a>
        </p>

        <p>Enjoy your meal! 🍽️<br/>
        – CampusEats</p>
        """;

        await _emailSender.SendAsync(
            to: evt.Email,
            subject: "Your order is on the way 🚴",
            body: body);
    }
}
using CampusEatsv2.Infrastructure.Messaging.Events;
using CampusEatsv2.Notification.Services;

namespace CampusEatsv2.Notification.Consumers;
// Consumer contains the Email that is sent the smpt4dev
// The only thing is does is consume a event when handle event occurs from RabbitMQ
// This one is for OrderDelivered

public sealed class OrderDeliveredConsumer
{
    private readonly SmtpEmailSender _emailSender;

    public OrderDeliveredConsumer(SmtpEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task Handle(OrderDeliveredIntegrationEvent evt)
    {
        var body = $"""
        <p>Hi <strong>Hi! {evt.FullName}</strong>,</p>

        <p>Your order has been <strong>successfully delivered</strong> ✅</p>

        <h3>Receipt</h3>
        <ul>
            <li>Total price: <strong>{evt.TotalPrice:0.00} kr</strong></li>
            <li>Delivery fee: {evt.DeliveryFee:0.00} kr</li>
            <li>Tip: {evt.Tip:0.00} kr</li>
        </ul>

        <p>Thank you for ordering with CampusEats 💚</p>

        <p>
            Support: 
            <a href="mailto:no.reply.campuseats@gmail.com">
                no.reply.campuseats@gmail.com
            </a>
        </p>

        <p>– CampusEats</p>
        """;

        await _emailSender.SendAsync(
            to: evt.Email,
            subject: "Your CampusEats receipt ✅",
            body: body);
    }
}
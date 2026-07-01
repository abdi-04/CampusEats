using CampusEatsv2.Notification.Services;
using CampusEatsv2.IntegrationTests.Helper;
using NUnit.Framework;
using CampusEatsv2.Infrastructure.Messaging.Events;

namespace CampusEatsv2.IntegrationTests;
// Here we test that the actual email sending gets sent! 
// We dont check if you can view it in the inbox as that is not the problem of the service
// During visual testing we saw that you get the email in inbox

[TestFixture]
[Category("Integration")]
public class OrderDeliveredEmailTests
{
    private List<(string To, string Subject, string Body)> _sent = null!;

    [SetUp]
    public void Setup()
    {
        _sent = new();
        SmtpEmailSender.OnSend = (to, subject, body) =>
            _sent.Add((to, subject, body));
    }

    [TearDown]
    public void TearDown()
    {
        SmtpEmailSender.OnSend = null;
    }

    [Test]
    public async Task OrderDelivered_Sends_Receipt_Email()
    {
        IntegrationTestPublisher.PublishIntegrationEvent(
            routingKey: "order.delivered",
            payload: new OrderDeliveredIntegrationEvent
            (
                OrderId : Guid.NewGuid(),
                FullName : "Integration Customer",
                Email : "integration.receipt@test.com",
                TotalPrice : 420m,
                DeliveryFee : 29,
                Tip : 10m

            ));

        await EmailAssertHelper.AssertEmailSentAsync(
            predicate: e =>
                e.To == "integration.receipt@test.com" &&
                e.Subject.Contains("receipt", StringComparison.OrdinalIgnoreCase),
            sent: _sent);
    }
}
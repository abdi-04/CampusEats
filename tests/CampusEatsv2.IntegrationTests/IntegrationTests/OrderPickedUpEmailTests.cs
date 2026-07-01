using CampusEatsv2.Notification.Services;
using CampusEatsv2.IntegrationTests.Helper;
using CampusEatsv2.Infrastructure.Messaging.Events;
using NUnit.Framework;

namespace CampusEatsv2.IntegrationTests;
// Here we test that the actual email sending gets sent! 
// We dont check if you can view it in the inbox as that is not the problem of the service
// During visual testing we saw that you get the email in inbox

[TestFixture]
[Category("Integration")]
public class OrderPickedUpEmailTests
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
    public async Task OrderPickedUp_Sends_OnTheWay_Email()
    {
        IntegrationTestPublisher.PublishIntegrationEvent(
            routingKey: "order.pickedup",
            payload: new OrderPickedUpIntegrationEvent
            (
                OrderId : Guid.NewGuid(),
                FullName :  "Integration Customer",
                Email :  "integration.order@test.com"
            ));

        
        await EmailAssertHelper.AssertEmailSentAsync(
            e =>
                e.To == "integration.order@test.com",
            sent: _sent
        );
    }
}
using CampusEatsv2.Notification.Services;
using CampusEatsv2.IntegrationTests.Helper;
using NUnit.Framework;

namespace CampusEatsv2.IntegrationTests;
// Here we test that the actual email sending gets sent! 
// We dont check if you can view it in the inbox as that is not the problem of the service
// During visual testing we saw that you get the email in inbox

[TestFixture]
[Category("Integration")]
public class CourierApprovedEmailTests
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
    public async Task CourierApproved_Sends_Approval_Email()
    {
        IntegrationTestPublisher.PublishIntegrationEvent(
            routingKey: "courier.approved",
            payload: new
            {
                CourierId = Guid.NewGuid(),
                FullName = "Integration Courier",
                Email = "integration.courier@test.com"
            });

        await EmailAssertHelper.AssertEmailSentAsync(
            predicate: e =>
                e.To == "integration.courier@test.com" &&
                e.Subject.Contains("approved", StringComparison.OrdinalIgnoreCase),
            sent: _sent);
    }
}
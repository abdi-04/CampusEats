using NUnit.Framework;

// Here we assert that an actual email with text was sent
// We make this helper since all integration tests want to see this!

public static class EmailAssertHelper
{
    public static async Task AssertEmailSentAsync(
        Func<(string To, string Subject, string Body), bool> predicate,
        List<(string To, string Subject, string Body)> sent,
        int timeoutMs = 5000)
    {
        var start = DateTime.UtcNow;

        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (sent.Any(predicate))
                return;

            await Task.Delay(100);
        }

        Assert.Fail("Expected email was not sent.");
    }
}
using System.Net.Mail;

namespace CampusEatsv2.Notification.Services;

/// <summary>
/// SMTP-based email sender used for sending notification emails.
/// 
/// This service is responsible for composing and sending emails using
/// SMTP configuration provided via application settings.
/// </summary>

public sealed class SmtpEmailSender
{
    public static Action<string, string, string>? OnSend;

    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IConfiguration config,
        ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        // Notify tests (if attached)
        OnSend?.Invoke(to, subject, body);

        var host = _config["Smtp:Host"];
        var portString = _config["Smtp:Port"];

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("SMTP host is not configured (Smtp:Host)");

        if (string.IsNullOrWhiteSpace(portString))
            throw new InvalidOperationException("SMTP port is not configured (Smtp:Port)");

        if (!int.TryParse(portString, out var port))
            throw new InvalidOperationException($"Invalid SMTP port value: {portString}");

        _logger.LogWarning(
            " SMTP SEND CALLED → {Host}:{Port} → {To}",
            host, port, to);

        using var client = new SmtpClient(host, port);
        using var message = new MailMessage
        {
            From = new MailAddress("no.reply.campuseats@gmail.com", "CampusEats"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(to);

        await client.SendMailAsync(message);

        _logger.LogInformation(" SMTP email sent to {Email}", to);
    }
}
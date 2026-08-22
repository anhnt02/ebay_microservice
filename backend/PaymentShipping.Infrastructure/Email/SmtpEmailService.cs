using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentShipping.Application.Notifications;

namespace PaymentShipping.Infrastructure.Email;

/// <summary>
/// SMTP email service — gửi email qua MailHog (dev) hoặc SMTP server thật (prod).
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string? _username;
    private readonly string? _password;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _logger   = logger;
        _host     = config["Email:Host"]      ?? "localhost";
        _port     = int.Parse(config["Email:Port"] ?? "1025");
        _username = config["Email:Username"];
        _password = config["Email:Password"];
        _fromEmail = config["Email:FromEmail"] ?? "noreply@paymentshipping.local";
        _fromName  = config["Email:FromName"]  ?? "PaymentShipping System";
        _enableSsl = bool.Parse(config["Email:EnableSsl"] ?? "false");
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = _enableSsl,
                Credentials = !string.IsNullOrWhiteSpace(_username)
                    ? new NetworkCredential(_username, _password)
                    : null,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 10_000
            };

            var from = new MailAddress(_fromEmail, _fromName);
            var to   = new MailAddress(toEmail);

            using var mail = new MailMessage(from, to)
            {
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true
            };

            await client.SendMailAsync(mail, ct);

            _logger.LogInformation(
                "Email sent | to={to} | subject={subject}",
                toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email | to={to} | subject={subject}",
                toEmail, subject);
            // Don't rethrow — email failures should not break the main flow
        }
    }
}

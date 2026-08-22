namespace PaymentShipping.Application.Notifications;

/// <summary>Service gửi email qua SMTP.</summary>
public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

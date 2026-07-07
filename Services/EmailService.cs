using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace AerodyneCompressors.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendHtmlEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            throw new InvalidOperationException("SMTP settings are not fully configured. Set SmtpSettings__Server, SmtpSettings__SenderEmail, and SmtpSettings__Password environment variables.");
        }

        if (string.IsNullOrWhiteSpace(message.To))
        {
            throw new ArgumentException("Recipient email address is required.", nameof(message));
        }

        var receiverEmail = message.To;
        var receiverName = message.ToName;

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(string.IsNullOrWhiteSpace(receiverName)
                ? new MailAddress(receiverEmail)
                : new MailAddress(receiverEmail, receiverName));

            if (!string.IsNullOrWhiteSpace(message.ReplyToEmail))
            {
                mailMessage.ReplyToList.Add(string.IsNullOrWhiteSpace(message.ReplyToName)
                    ? new MailAddress(message.ReplyToEmail)
                    : new MailAddress(message.ReplyToEmail, message.ReplyToName));
            }

            using var smtpClient = new SmtpClient(_settings.Server, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password),
                EnableSsl = _settings.EnableSsl
            };

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Recipient}", receiverEmail);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error while sending email to {Recipient}. Status: {StatusCode}", receiverEmail, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending email to {Recipient}", receiverEmail);
            throw;
        }
    }
}

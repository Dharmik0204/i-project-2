using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

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
            throw new InvalidOperationException(
                "SMTP settings are not configured. Set SmtpSettings__Server, SmtpSettings__SenderEmail, and SmtpSettings__Password.");
        }

        if (string.IsNullOrWhiteSpace(message.To))
        {
            throw new ArgumentException("Recipient email address is required.", nameof(message));
        }

        var mimeMessage = BuildMimeMessage(message);
        var portsToTry = GetPortsToTry();

        Exception? lastError = null;

        foreach (var port in portsToTry)
        {
            try
            {
                using var client = new SmtpClient { Timeout = 30000 };

                var secureSocketOptions = GetSecureSocketOptions(port);
                await client.ConnectAsync(_settings.Server.Trim(), port, secureSocketOptions, cancellationToken);
                await client.AuthenticateAsync(_settings.SenderEmail.Trim(), _settings.Password, cancellationToken);
                await client.SendAsync(mimeMessage, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Email sent via SMTP ({Server}:{Port}) to {Recipient}", _settings.Server, port, message.To);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "SMTP send failed on port {Port}", port);
            }
        }

        _logger.LogError(lastError, "SMTP delivery failed for {Recipient}", message.To);
        throw new InvalidOperationException("SMTP delivery failed. Verify Gmail App Password and SMTP settings.", lastError);
    }

    private IEnumerable<int> GetPortsToTry()
    {
        if (_settings.Port == 465)
        {
            yield return 465;
            yield break;
        }

        yield return _settings.Port;

        if (_settings.Port == 587)
        {
            yield return 465;
        }
    }

    private SecureSocketOptions GetSecureSocketOptions(int port)
    {
        if (port == 465)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        return _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
    }

    private MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_settings.SenderName.Trim(), _settings.SenderEmail.Trim()));
        mimeMessage.To.Add(string.IsNullOrWhiteSpace(message.ToName)
            ? MailboxAddress.Parse(message.To.Trim())
            : new MailboxAddress(message.ToName, message.To.Trim()));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new TextPart("html") { Text = message.HtmlBody };

        if (!string.IsNullOrWhiteSpace(message.ReplyToEmail))
        {
            mimeMessage.ReplyTo.Add(string.IsNullOrWhiteSpace(message.ReplyToName)
                ? MailboxAddress.Parse(message.ReplyToEmail.Trim())
                : new MailboxAddress(message.ReplyToName, message.ReplyToEmail.Trim()));
        }

        return mimeMessage;
    }
}

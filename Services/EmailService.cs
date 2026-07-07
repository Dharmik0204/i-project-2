using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AerodyneCompressors.Services;

public class EmailService : IEmailService
{
    private const int SmtpTimeoutMs = 10000;

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
                "SMTP is not configured. Set SmtpSettings__SenderEmail and SmtpSettings__Password in Render Environment variables.");
        }

        if (string.IsNullOrWhiteSpace(message.To))
        {
            throw new ArgumentException("Recipient email address is required.", nameof(message));
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(SmtpTimeoutMs + 2000));

        var mimeMessage = BuildMimeMessage(message);

        try
        {
            using var client = new SmtpClient
            {
                Timeout = SmtpTimeoutMs
            };

            var port = _settings.Port;
            var secureSocketOptions = GetSecureSocketOptions(port);

            _logger.LogInformation(
                "Connecting to SMTP {Server}:{Port} as {Sender}",
                _settings.Server,
                port,
                _settings.SenderEmail);

            await client.ConnectAsync(
                _settings.Server.Trim(),
                port,
                secureSocketOptions,
                timeoutCts.Token);

            await client.AuthenticateAsync(
                _settings.SenderEmail.Trim(),
                _settings.Password,
                timeoutCts.Token);

            await client.SendAsync(mimeMessage, timeoutCts.Token);
            await client.DisconnectAsync(true, timeoutCts.Token);

            _logger.LogInformation("Email sent via SMTP to {Recipient}", message.To);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "SMTP timed out connecting to {Server}:{Port}", _settings.Server, _settings.Port);
            throw new InvalidOperationException(
                "Email server connection timed out. On Render free plan SMTP is blocked — upgrade to Starter plan. Also verify Gmail App Password.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP failed for {Recipient}", message.To);
            throw new InvalidOperationException(
                "SMTP delivery failed. Use a Gmail App Password (not your normal password) and verify Render SMTP environment variables.",
                ex);
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

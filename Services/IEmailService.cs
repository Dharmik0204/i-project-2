namespace AerodyneCompressors.Services;

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? ReplyToEmail { get; set; }
    public string? ReplyToName { get; set; }
}

public interface IEmailService
{
    Task SendHtmlEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

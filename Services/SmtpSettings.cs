namespace AerodyneCompressors.Services;

public class SmtpSettings
{
    public const string SectionName = "SmtpSettings";

    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string ReceiverEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Server) &&
        !string.IsNullOrWhiteSpace(SenderEmail) &&
        !string.IsNullOrWhiteSpace(Password);
}

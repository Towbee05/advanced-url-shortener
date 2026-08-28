namespace UrlShortener.Entities;

public class SMTPSettings
{
    public string EMAIL_HOST { get; set; } = string.Empty;
    public string EMAIL_PASSWORD { get; set; } = string.Empty;
    public int EMAIL_PORT { get; set; }
    public string EMAIL_USERNAME { get; set; } = string.Empty;
    public string EMAIL_SENDER { get; set; } = string.Empty;
}
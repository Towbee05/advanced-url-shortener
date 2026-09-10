namespace UrlShortener.DTO.Response;

public class VerificationEmailModel
{
    public string Username { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public int ExpiryMinute { get; set; }
}
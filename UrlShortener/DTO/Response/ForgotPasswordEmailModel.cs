namespace UrlShortener.DTO.Response;

public class ForgotPasswordEmailModel
{
    public string Username { get; set; } = string.Empty;
    public string ResetLink { get; set; } = string.Empty;
    public int ExpiryMinute { get; set; }
}
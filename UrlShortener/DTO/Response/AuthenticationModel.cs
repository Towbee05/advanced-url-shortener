namespace UrlShortener.DTO.Response;

public class AuthenticationModel
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
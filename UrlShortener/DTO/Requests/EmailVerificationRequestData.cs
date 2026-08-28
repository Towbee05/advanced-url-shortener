namespace UrlShortener.DTO.Requests;

public class EmailVerificationRequestData
{
    public string EmailAddress { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
}
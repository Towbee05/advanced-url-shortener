namespace UrlShortener.DTO.Response;

public class ErrorResponse
{
    public string Status { get; } = "error";
    public string Details { get; set; } = string.Empty;
}
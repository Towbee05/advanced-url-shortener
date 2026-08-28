namespace UrlShortener.Models;

public class Urls
{
    public Guid Id { get; set; }
    public Guid UserId {get; set;}
    public string OriginalUrl { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}
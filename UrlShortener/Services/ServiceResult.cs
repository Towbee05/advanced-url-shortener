namespace UrlShortener.Services;

public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public int? ErrorCode { get; set; }
}
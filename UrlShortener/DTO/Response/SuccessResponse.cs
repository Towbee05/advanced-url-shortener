using System.Text.Json.Serialization;

namespace UrlShortener.DTO.Response;

public class SuccessResponse<T>
{
    public string Status { get; } = "success";
    public T? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
}
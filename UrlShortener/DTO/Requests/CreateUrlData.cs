using System.ComponentModel.DataAnnotations;

namespace UrlShortener.DTO.Requests;

public class CreateUrlData
{
    [Required(ErrorMessage = "Please provide a long url")]
    public string LongUrl { get; set; } = string.Empty;
}
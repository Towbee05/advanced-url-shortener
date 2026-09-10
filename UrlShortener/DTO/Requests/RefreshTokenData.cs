using System.ComponentModel.DataAnnotations;

namespace UrlShortener.DTO.Requests;

public class RefreshTokenData
{
    [Required(ErrorMessage = "please provide a refresh token")]
    public string RefreshToken { get; set; } = string.Empty;
}
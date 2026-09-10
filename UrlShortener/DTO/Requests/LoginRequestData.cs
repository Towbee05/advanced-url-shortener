using System.ComponentModel.DataAnnotations;

namespace UrlShortener.DTO.Requests;
public class LoginRequestData
{
    [Required(ErrorMessage = "please provide an email address")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "please provide a password")]
    public string Password { get; set; } = string.Empty;
}
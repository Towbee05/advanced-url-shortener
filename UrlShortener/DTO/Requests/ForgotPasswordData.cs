using System.ComponentModel.DataAnnotations;

namespace UrlShortener.DTO.Requests;

public class ForgotPasswordData
{
    [Required(ErrorMessage = "please provide an email address")]
    public string EmailAddress { get; set; } = string.Empty;
}
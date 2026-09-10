using System.ComponentModel.DataAnnotations;

namespace UrlShortener.DTO.Requests;

public class EmailVerificationRequestData
{
    [Required(ErrorMessage = "please provide an email address")]
    public string EmailAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "please provide a verification code")]
    public string VerificationCode { get; set; } = string.Empty;
}
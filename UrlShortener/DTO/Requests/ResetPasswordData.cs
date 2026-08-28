using System.ComponentModel.DataAnnotations;

namespace UrlShortener.DTO.Requests;

public class ResetPasswordData
{
    [Required(ErrorMessage = "please provide an email address")]
    public string EmailAddress { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "please provide a reset token")]
    public string ResetToken { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "please provide a new password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "confirm password field is required")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

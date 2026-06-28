using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

public class RegisterDto
{
    [Required, MinLength(2), MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8),
     RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
         ErrorMessage = "Password must be at least 8 characters and include uppercase, lowercase, a digit, and a special character.")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

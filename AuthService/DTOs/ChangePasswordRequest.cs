using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class ChangePasswordRequest
{
    [EmailAddress]
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string OldPassword { get; set; } = string.Empty;
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
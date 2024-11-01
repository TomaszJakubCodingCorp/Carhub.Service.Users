using System.ComponentModel.DataAnnotations;

namespace Carhub.Service.Users.Core.DTOs;

public sealed record ResetPasswordDto
{
    [Required] public string ResetToken { get; init; } = string.Empty;
    [Required] [MaxLength(100)] public string NewPassword { get; init; } = string.Empty;
}
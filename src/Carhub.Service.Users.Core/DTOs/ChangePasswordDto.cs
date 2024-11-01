using System.ComponentModel.DataAnnotations;

namespace Carhub.Service.Users.Core.DTOs;

public sealed class ChangePasswordDto
{
    [Required] [MaxLength(100)] public string OldPassword { get; init; } = string.Empty;
    [Required] [MaxLength(100)] public string NewPassword { get; init; } = string.Empty;
    public Guid UserId { get; set; }
}
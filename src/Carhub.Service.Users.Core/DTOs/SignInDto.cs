using System.ComponentModel.DataAnnotations;

namespace Carhub.Service.Users.Core.DTOs;

public sealed record SignInDto
{
    [EmailAddress] [Required] [MaxLength(400)] public string Email { get; init; } = string.Empty;

    [Required] [MaxLength(100)] public string Password { get; init; } = string.Empty;
}
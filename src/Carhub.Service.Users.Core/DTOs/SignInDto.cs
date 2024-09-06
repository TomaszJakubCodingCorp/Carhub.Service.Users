using System.ComponentModel.DataAnnotations;

namespace Carhub.Service.Users.Core.DTOs;

public sealed record SignInDto
{
    [EmailAddress] [Required] public string Email { get; init; } = string.Empty;

    [Required] public string Password { get; init; } = string.Empty;
}
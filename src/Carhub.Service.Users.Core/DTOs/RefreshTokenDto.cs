using System.ComponentModel.DataAnnotations;

namespace Carhub.Service.Users.Core.DTOs;

public sealed class RefreshTokenDto
{
    [Required] public string Jwt { get; init; }
    [Required] public string RefreshToken { get; init; }
}
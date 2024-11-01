using System.ComponentModel.DataAnnotations;

namespace Carhub.Service.Users.Core.DTOs;

public sealed class RequestResetPasswordTokenDto
{
    [Required] public string Email { get; init; } = string.Empty;
}
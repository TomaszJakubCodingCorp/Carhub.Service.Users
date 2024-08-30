using System.ComponentModel.DataAnnotations;

namespace Carhub.Service.Users.Core.DTOs;

public sealed record SignUpDto
{
    public Guid Id { get; init; }

    [EmailAddress]
    [Required]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public Dictionary<string, IEnumerable<string>> Claims { get; init; } = [];
}
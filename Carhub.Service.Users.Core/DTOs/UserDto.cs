namespace Carhub.Service.Users.Core.DTOs;

public sealed record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public Dictionary<string, IEnumerable<string>> Claims { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
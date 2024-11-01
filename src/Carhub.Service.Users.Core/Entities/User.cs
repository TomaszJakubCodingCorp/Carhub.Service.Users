namespace Carhub.Service.Users.Core.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ActivationToken { get; set; }
    public DateTime? ActivationTokenExpires { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordTokenExpires { get; set; }
    public bool Banned { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, IEnumerable<string>> Claims { get; set; } = [];
}
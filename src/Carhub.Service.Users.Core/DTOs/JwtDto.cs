namespace Carhub.Service.Users.Core.DTOs;

public sealed class JwtDto
{
    public Token AccessToken { get; set; }
    public Token RefreshToken { get; set; }
    public string Id { get; set; }
    public string Role { get; set; }
    public string Email { get; set; }
    public IDictionary<string, IEnumerable<string>> Claims { get; set; }
}
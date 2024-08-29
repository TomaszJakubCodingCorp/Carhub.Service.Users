namespace Carhub.Service.Users.Core.DTOs;

public sealed record JwtDto(string AccessToken, long Expires, string Id, string Role,
    string Email, Dictionary<string, IEnumerable<string>> Claims);
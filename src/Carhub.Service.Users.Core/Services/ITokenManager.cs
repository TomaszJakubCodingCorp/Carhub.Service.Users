using System.Security.Claims;
using Carhub.Service.Users.Core.DTOs;

namespace Carhub.Service.Users.Core.Services;

public interface ITokenManager
{
    Task<JwtDto> CreateTokenAsync(string userId, string email, string? role = null, string? audience = null,
        IDictionary<string, IEnumerable<string>>? claims = null);

    Task ValidRefreshAsync(string userId, string refreshToken);

    ClaimsPrincipal GetPrincipalFromToken(string token);
    Token GenerateActivationToken();
}
using Carhub.Service.Users.Core.DTOs;

namespace Carhub.Service.Users.Core.Services;

internal interface ITokenManager
{
    JwtDto CreateToken(string userId, string email, string? role = null, string? audience = null,
        IDictionary<string, IEnumerable<string>>? claims = null);
}
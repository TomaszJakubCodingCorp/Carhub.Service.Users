using System.Text;
using Carhub.Service.Users.Core.DTOs;
using Carhub.Service.Users.Core.Exceptions;
using Carhub.Service.Users.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Carhub.Service.Users.Core.Services;

internal sealed class TokenManager: ITokenManager
{
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;

    public TokenManager(IOptions<AuthOptions> authOptions,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(authOptions.Value.IssuerSigningKey))
            throw new InvalidOperationException("Issuer signing key is not set.");

        _timeProvider = timeProvider;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.Value.IssuerSigningKey)),
            SecurityAlgorithms.Sha512);
    }

    public JwtDto CreateToken(string userId, string email, string? role = null, string? audience = null,
        IDictionary<string, IEnumerable<string>>? claims = null)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId == Guid.Empty.ToString())
            throw new MissingUserIdException();
        
        //TODO: complete the method
        throw new NotImplementedException();
    }
}
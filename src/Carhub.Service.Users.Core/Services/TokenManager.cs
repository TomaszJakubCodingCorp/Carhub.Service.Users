using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Carhub.Service.Users.Core.DTOs;
using Carhub.Service.Users.Core.Exceptions;
using Carhub.Service.Users.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Carhub.Service.Users.Core.Services;

internal sealed class TokenManager : ITokenManager
{
    private readonly AuthOptions _authOptions;
    private readonly Dictionary<string, IEnumerable<string>> _emptyClaims = new();
    private readonly SigningCredentials _signingCredentials;
    private readonly TimeProvider _timeProvider;

    public TokenManager(IOptions<AuthOptions> authOptions,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(authOptions.Value.IssuerSigningKey))
            throw new InvalidOperationException("Issuer signing key is not set.");

        _timeProvider = timeProvider;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.Value.IssuerSigningKey)),
            SecurityAlgorithms.HmacSha512);
        _authOptions = authOptions.Value;
    }

    public JwtDto CreateToken(string userId, string email, string? role = null, string? audience = null,
        IDictionary<string, IEnumerable<string>>? claims = null)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId == Guid.Empty.ToString())
            throw new MissingUserIdException();

        var now = _timeProvider.GetLocalNow().DateTime;
        var jwtClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.UniqueName, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeMilliseconds().ToString())
        };

        if (!string.IsNullOrWhiteSpace(role))
            jwtClaims.Add(new Claim(ClaimTypes.Role, role));

        if (!string.IsNullOrWhiteSpace(audience))
            jwtClaims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));

        if (claims?.Any() is true)
        {
            var customClaims = new List<Claim>();
            foreach (var (key, values) in claims) customClaims.AddRange(values.Select(value => new Claim(key, value)));
            jwtClaims.AddRange(customClaims);
        }

        var expires = now.Add(_authOptions.Expiry);

        var jwt = new JwtSecurityToken(
            _authOptions.Issuer,
            claims: jwtClaims,
            notBefore: now,
            expires: expires,
            signingCredentials: _signingCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new JwtDto(token, new DateTimeOffset(expires).ToUnixTimeMilliseconds(), userId,
            role ?? string.Empty, email, claims ?? _emptyClaims);
    }
}
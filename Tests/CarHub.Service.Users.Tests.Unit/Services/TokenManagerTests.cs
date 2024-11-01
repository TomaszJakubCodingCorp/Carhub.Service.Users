using Carhub.Service.Users.Core.Exceptions;
using Carhub.Service.Users.Core.Options;
using Carhub.Service.Users.Core.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace CarHub.Service.Users.Tests.Unit.Services;

public class TokenManagerTests
{
    private readonly IOptions<AuthOptions> _authOptions;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly TokenManager _tokenManager;

    public TokenManagerTests()
    {
        _authOptions = Substitute.For<IOptions<AuthOptions>>();
        _timeProvider = new FakeTimeProvider();
        _timeProvider.SetUtcNow(new DateTimeOffset(new DateTime(2024, 9, 2)));
        _authOptions.Value.Returns(new AuthOptions
        {
            Issuer = "testIssuer",
            IssuerSigningKey = "testSigningKeytestSigningKeytestSigningKeytestSigningKeytestSigningKey" +
                               "testSigningKeytestSigningKeytestSigningKeytestSigningKeytestSigningKey" +
                               "testSigningKeytestSigningKeytestSigningKeytestSigningKeytestSigningKey",
            Expiry = TimeSpan.FromHours(1),
            RefreshExpiry = TimeSpan.FromDays(7),
            ActivationExpiry = TimeSpan.FromDays(14)
        });
        _refreshTokenService = Substitute.For<IRefreshTokenService>();
        _tokenManager = new TokenManager(_authOptions, _timeProvider, _refreshTokenService);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenIssuerSigningKeyIsNotSet()
    {
        // Arrange
        _authOptions.Value.Returns(new AuthOptions
        {
            Issuer = "testIssuer",
            IssuerSigningKey = null!, // Invalid key
            Expiry = TimeSpan.FromHours(1)
        });

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            new TokenManager(_authOptions, _timeProvider, _refreshTokenService));
    }

    [Fact]
    public void CreateToken_ShouldThrowException_WhenUserIdIsEmpty()
    {
        // Act & Assert
        Should.Throw<MissingUserIdException>(() => _tokenManager.CreateTokenAsync(string.Empty, "test@example.com"));
    }

    [Fact]
    public async Task CreateToken_ShouldCreateJwtDto_WithValidData()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var role = "Admin";
        var audience = "testAudience";
        var claims = new Dictionary<string, IEnumerable<string>>
        {
            { "customClaimType", new[] { "customClaimValue1", "customClaimValue2" } }
        };

        // Act
        var result = await _tokenManager.CreateTokenAsync(userId, email, role, audience, claims);

        var expires = _timeProvider.GetLocalNow().AddHours(1).ToUnixTimeMilliseconds();
        var refreshExpires = _timeProvider.GetLocalNow().AddDays(7).ToUnixTimeMilliseconds();
        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNull();
        result.AccessToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        result.AccessToken.TokenExpires.ShouldBe(expires);
        result.RefreshToken.ShouldNotBeNull();
        result.RefreshToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.TokenExpires.ShouldBe(refreshExpires);
        result.Id.ShouldBe(userId);
        result.Role.ShouldBe(role);
        result.Email.ShouldBe(email);
        result.Claims.ShouldContainKey("customClaimType");
        result.Claims["customClaimType"].ShouldContain("customClaimValue1");
        result.Claims["customClaimType"].ShouldContain("customClaimValue2");
    }

    [Fact]
    public async Task CreateToken_ShouldHandleNullRoleAndAudience()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var claims = new Dictionary<string, IEnumerable<string>>();

        // Act
        var result = await _tokenManager.CreateTokenAsync(userId, email, null, null, claims);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNull();
        result.AccessToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.ShouldNotBeNull();
        result.RefreshToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        result.Role.ShouldBe(string.Empty);
        result.Claims.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidRefreshAsync_ShouldNotThrowException_WhenRefreshTokenIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var refreshToken = "validRefreshToken";

        _refreshTokenService.GetRefreshTokenAsync(userId).Returns(refreshToken);

        // Act & Assert
        await _tokenManager.ValidRefreshAsync(userId, refreshToken);
        await _refreshTokenService.Received(1).GetRefreshTokenAsync(userId);
    }

    [Fact]
    public async Task ValidRefreshAsync_ShouldThrowInvalidRefreshTokenException_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var storedToken = "storedToken";
        var providedToken = "invalidRefreshToken";

        _refreshTokenService.GetRefreshTokenAsync(userId).Returns(storedToken);

        // Act & Assert
        await Should.ThrowAsync<InvalidRefreshTokenException>(() =>
            _tokenManager.ValidRefreshAsync(userId, providedToken));
        await _refreshTokenService.Received(1).GetRefreshTokenAsync(userId);
    }

    [Fact]
    public void GetPrincipalFromToken_ShouldThrowInvalidJwtTokenException_WhenTokenIsInvalid()
    {
        // Arrange
        var invalidToken = "invalidToken";

        // Act & Assert
        Should.Throw<InvalidJwtTokenException>(() => _tokenManager.GetPrincipalFromToken(invalidToken));
    }

    [Fact]
    public void GenerateActivationToken_ShouldReturnValidToken_WithCorrectExpiry()
    {
        // Act
        var result = _tokenManager.GenerateActivationToken();

        var expectedExpiry = _timeProvider.GetLocalNow().Add(_authOptions.Value.ActivationExpiry)
            .ToUnixTimeMilliseconds();

        // Assert
        result.ShouldNotBeNull();
        result.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        result.TokenExpires.ShouldBe(expectedExpiry);
    }

    [Fact]
    public async Task CreateToken_ShouldCreateJwtDto_WithNoClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var role = "Admin";
        var audience = "testAudience";

        // Act
        var result = await _tokenManager.CreateTokenAsync(userId, "", role, audience);

        var expires = _timeProvider.GetLocalNow().AddHours(1).ToUnixTimeMilliseconds();
        var refreshExpires = _timeProvider.GetLocalNow().AddDays(7).ToUnixTimeMilliseconds();

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNull();
        result.AccessToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        result.AccessToken.TokenExpires.ShouldBe(expires);
        result.RefreshToken.ShouldNotBeNull();
        result.RefreshToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.TokenExpires.ShouldBe(refreshExpires);
        result.Id.ShouldBe(userId);
        result.Role.ShouldBe(role);
        result.Claims.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateToken_ShouldCreateJwtDto_WithNoRoleAndClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var audience = "testAudience";

        // Act
        var result = await _tokenManager.CreateTokenAsync(userId, "", audience: audience);

        var expires = _timeProvider.GetLocalNow().AddHours(1).ToUnixTimeMilliseconds();
        var refreshExpires = _timeProvider.GetLocalNow().AddDays(7).ToUnixTimeMilliseconds();

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNull();
        result.AccessToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        result.AccessToken.TokenExpires.ShouldBe(expires);
        result.RefreshToken.ShouldNotBeNull();
        result.RefreshToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.TokenExpires.ShouldBe(refreshExpires);
        result.Id.ShouldBe(userId);
        result.Role.ShouldBe(string.Empty); // Expect empty role
        result.Claims.ShouldBeEmpty();
    }
}
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
            Expiry = TimeSpan.FromHours(1)
        });

        _tokenManager = new TokenManager(_authOptions, _timeProvider);
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
        Should.Throw<InvalidOperationException>(() => new TokenManager(_authOptions, _timeProvider));
    }

    [Fact]
    public void CreateToken_ShouldThrowException_WhenUserIdIsEmpty()
    {
        // Act & Assert
        Should.Throw<MissingUserIdException>(() => _tokenManager.CreateToken(string.Empty, "test@example.com"));
    }

    [Fact]
    public void CreateToken_ShouldCreateJwtDto_WithValidData()
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
        var result = _tokenManager.CreateToken(userId, email, role, audience, claims);

        var expires = _timeProvider.GetLocalNow().AddHours(1).ToUnixTimeMilliseconds();
        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNullOrEmpty();
        result.Expires.ShouldBe(expires);
        result.Id.ShouldBe(userId);
        result.Role.ShouldBe(role);
        result.Email.ShouldBe(email);
        result.Claims.ShouldContainKey("customClaimType");
        result.Claims["customClaimType"].ShouldContain("customClaimValue1");
        result.Claims["customClaimType"].ShouldContain("customClaimValue2");
    }

    [Fact]
    public void CreateToken_ShouldHandleNullRoleAndAudience()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var claims = new Dictionary<string, IEnumerable<string>>();

        // Act
        var result = _tokenManager.CreateToken(userId, email, role: null, audience: null, claims);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNullOrEmpty();
        result.Role.ShouldBe(string.Empty);
        result.Claims.ShouldBeEmpty();
    }
}
using Carhub.Service.Users.Core.Exceptions;
using Carhub.Service.Users.Core.Services;
using Shouldly;

namespace CarHub.Service.Users.Tests.Unit.Services;

public class PasswordManagerTests
{
    private readonly PasswordManager _passwordManager = new();

    [Fact]
    public void CreatePasswordHash_ShouldGenerateHashAndSalt()
    {
        // Arrange
        var password = "password1234";

        // Act
        _passwordManager.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);

        // Assert
        passwordHash.ShouldNotBeNull();
        passwordSalt.ShouldNotBeNull();
        passwordHash.Length.ShouldBe(64); // HMACSHA512 hash length
        passwordSalt.Length.ShouldBe(128); // HMACSHA512 key length
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrueForValidPassword()
    {
        // Arrange
        var password = "password123";
        _passwordManager.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);

        // Act
        var result = _passwordManager.VerifyPassword(password, passwordHash, passwordSalt);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalseForInvalidPassword()
    {
        // Arrange
        var password = "password123";
        var incorrectPassword = "wrongpassword";
        _passwordManager.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);

        // Act
        var result = _passwordManager.VerifyPassword(incorrectPassword, passwordHash, passwordSalt);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("short", "Password must be at least 8 characters long.\n")]
    [InlineData("nouppercase1!", "Password must contain at least one uppercase letter.\n")]
    [InlineData("NOLOWERCASE1!", "Password must contain at least one lowercase letter.\n")]
    [InlineData("NoDigit!", "Password must contain at least one digit.\n")]
    [InlineData("NoSpecialChar1", "Password must contain at least one special character.\n")]
    public void VerifyPasswordRequirements_ShouldThrowExceptionForInvalidPassword(string password,
        string expectedMessage)
    {
        // Act & Assert
        var ex = Should.Throw<PasswordRequirementsException>(
            () => _passwordManager.VerifyPasswordRequirements(password));
        ex.Message.ShouldContain(expectedMessage);
    }

    [Theory]
    [InlineData("Valid1Password!")]
    [InlineData("AnotherValid1!")]
    [InlineData("P@ssw0rd123")]
    public void VerifyPasswordRequirements_ShouldNotThrowExceptionForValidPassword(string password)
    {
        // Act & Assert
        Should.NotThrow(() => _passwordManager.VerifyPasswordRequirements(password));
    }

    [Fact]
    public void VerifyPasswordRequirements_ShouldThrowExceptionForEmptyPassword()
    {
        // Arrange
        var password = string.Empty;
        var expectedMessage = "Password must be at least 8 characters long.\n" +
                              "Password must contain at least one uppercase letter.\n" +
                              "Password must contain at least one lowercase letter.\n" +
                              "Password must contain at least one digit.\n" +
                              "Password must contain at least one special character.\n" +
                              "All the requirements must be fulfilled.";

        // Act
        var ex = Should.Throw<PasswordRequirementsException>(
            () => _passwordManager.VerifyPasswordRequirements(password));

        // Assert
        ex.Message.ShouldBe(expectedMessage);
    }
}
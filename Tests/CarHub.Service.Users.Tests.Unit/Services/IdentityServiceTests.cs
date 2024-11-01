using System.Security.Claims;
using Carhub.Service.Users.Core.DTOs;
using Carhub.Service.Users.Core.Entities;
using Carhub.Service.Users.Core.Exceptions;
using Carhub.Service.Users.Core.Repositories;
using Carhub.Service.Users.Core.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace CarHub.Service.Users.Tests.Unit.Services;

public sealed class IdentityServiceTests
{
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IdentityService _identityService;
    private readonly IPasswordManager _passwordManager;
    private readonly ITokenManager _tokenManager;
    private readonly IUserRepository _userRepository;

    public IdentityServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordManager = Substitute.For<IPasswordManager>();
        _tokenManager = Substitute.For<ITokenManager>();
        _fakeTimeProvider = new FakeTimeProvider();
        _fakeTimeProvider.SetUtcNow(new DateTimeOffset(new DateTime(2024, 9, 2)));

        _identityService = new IdentityService(
            _userRepository,
            _passwordManager,
            _tokenManager,
            _fakeTimeProvider);
    }

    #region GetAsync

    [Fact]
    public async Task GetAsync_ShouldReturnUserDto_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Role = "User",
            Firstname = "John",
            Lastname = "Doe",
            CreatedAt = _fakeTimeProvider.GetLocalNow().DateTime
        };
        _userRepository.GetAsync(userId).Returns(user);

        // Act
        var result = await _identityService.GetAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
        result.Email.ShouldBe(user.Email);
        await _userRepository.Received(1).GetAsync(userId);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetAsync(userId).Returns((User)null!);

        // Act
        var result = await _identityService.GetAsync(userId);

        // Assert
        result.ShouldBeNull();
        await _userRepository.Received(1).GetAsync(userId);
    }

    #endregion

    #region SignUpAsync

    [Fact]
    public async Task SignUpAsync_ShouldAddUser_WhenEmailIsNotInUse()
    {
        // Arrange
        var signUpDto = new SignUpDto
        {
            Id = Guid.NewGuid(),
            Email = "newuser@example.com",
            Password = "password123",
            Firstname = "John",
            Lastname = "Doe"
        };
        var expires = new DateTimeOffset(_fakeTimeProvider.GetLocalNow().DateTime.AddHours(1)).ToUnixTimeMilliseconds();
        _userRepository.GetAsync(signUpDto.Email.ToLowerInvariant()).Returns((User)null!);
        _tokenManager.GenerateActivationToken().Returns(new Token { GeneratedToken = "123", TokenExpires = expires });

        // Act
        await _identityService.SignUpAsync(signUpDto);

        // Assert
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email == signUpDto.Email &&
            u.Firstname == signUpDto.Firstname &&
            u.Lastname == signUpDto.Lastname &&
            u.IsActive == false));
    }

    [Fact]
    public async Task SignUpAsync_ShouldThrowEmailInUseException_WhenEmailIsAlreadyInUse()
    {
        // Arrange
        var signUpDto = new SignUpDto
        {
            Id = Guid.NewGuid(),
            Email = "existinguser@example.com",
            Password = "password123",
            Firstname = "John",
            Lastname = "Doe"
        };
        var existingUser = new User { Email = signUpDto.Email };
        _userRepository.GetAsync(signUpDto.Email.ToLowerInvariant()).Returns(existingUser);

        // Act & Assert
        await Should.ThrowAsync<EmailInUseException>(() => _identityService.SignUpAsync(signUpDto));
        await _userRepository.Received(1).GetAsync(signUpDto.Email.ToLowerInvariant());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>());
    }

    #endregion

    #region SignInAsync

    [Fact]
    public async Task SignInAsync_ShouldReturnJwtDto_WhenCredentialsAreValid()
    {
        // Arrange
        var signInDto = new SignInDto
        {
            Email = "validuser@example.com",
            Password = "validpassword123"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = signInDto.Email,
            PasswordHash = new byte[32],
            PasswordSalt = new byte[32],
            IsActive = true
        };

        var expires = new DateTimeOffset(_fakeTimeProvider.GetLocalNow().DateTime.AddHours(1)).ToUnixTimeMilliseconds();
        var jwt = new JwtDto
        {
            AccessToken = new Token
            {
                GeneratedToken = "some-jwt-token",
                TokenExpires = expires
            },
            RefreshToken = new Token
            {
                GeneratedToken = "abc",
                TokenExpires = expires
            },
            Role = user.Role,
            Email = user.Email,
            Claims = user.Claims,
            Id = user.Id.ToString()
        };

        _userRepository.GetAsync(signInDto.Email.ToLowerInvariant()).Returns(user);
        _passwordManager.VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt).Returns(true);
        _tokenManager.CreateTokenAsync(user.Id.ToString(), user.Email, user.Role, claims: user.Claims).Returns(jwt);

        // Act
        var result = await _identityService.SignInAsync(signInDto);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe(jwt.AccessToken);
        await _userRepository.Received(1).GetAsync(signInDto.Email.ToLowerInvariant());
        _passwordManager.Received(1).VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt);
        await _tokenManager.Received(1).CreateTokenAsync(user.Id.ToString(), user.Email, user.Role, claims: user
            .Claims);
    }

    [Fact]
    public async Task SignInAsync_ShouldThrowInvalidCredentialsException_WhenUserDoesNotExist()
    {
        // Arrange
        var signInDto = new SignInDto
        {
            Email = "nonexistentuser@example.com",
            Password = "password123"
        };
        _userRepository.GetAsync(signInDto.Email.ToLowerInvariant()).Returns((User)null!);

        // Act & Assert
        await Should.ThrowAsync<InvalidCredentialsException>(() => _identityService.SignInAsync(signInDto));
        await _userRepository.Received(1).GetAsync(signInDto.Email.ToLowerInvariant());
        _passwordManager.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<byte[]>());
        await _tokenManager.DidNotReceive().CreateTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg
                .Any<string>(),
            claims: Arg.Any<IDictionary<string, IEnumerable<string>>>());
    }

    [Fact]
    public async Task SignInAsync_ShouldThrowInvalidCredentialException_WhenPasswordIsIncorrect()
    {
        // Arrange
        var signInDto = new SignInDto
        {
            Email = "validuser@example.com",
            Password = "wrongpassword"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = signInDto.Email,
            PasswordHash = new byte[32],
            PasswordSalt = new byte[32],
            IsActive = true
        };
        _userRepository.GetAsync(signInDto.Email.ToLowerInvariant()).Returns(user);
        _passwordManager.VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt).Returns(false);

        // Act & Assert
        await Should.ThrowAsync<InvalidCredentialsException>(() => _identityService.SignInAsync(signInDto));
        await _userRepository.Received(1).GetAsync(signInDto.Email.ToLowerInvariant());
        _passwordManager.Received(1).VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt);
        await _tokenManager.DidNotReceive().CreateTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg
                .Any<string>(),
            claims: Arg.Any<IDictionary<string, IEnumerable<string>>>());
    }

    [Fact]
    public async Task SignInAsync_ShouldThrowUserNotActiveException_WhenUserIsNotActive()
    {
        // Arrange
        var signInDto = new SignInDto
        {
            Email = "inactiveuser@example.com",
            Password = "password123"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = signInDto.Email,
            PasswordHash = new byte[32],
            PasswordSalt = new byte[32],
            IsActive = false
        };
        _userRepository.GetAsync(signInDto.Email.ToLowerInvariant()).Returns(user);
        _passwordManager.VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt).Returns(true);

        // Act & Assert
        await Should.ThrowAsync<UserNotActiveException>(() => _identityService.SignInAsync(signInDto));
        await _userRepository.Received(1).GetAsync(signInDto.Email.ToLowerInvariant());
        _passwordManager.Received(1).VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt);
        await _tokenManager.DidNotReceive().CreateTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg
                .Any<string>(),
            claims: Arg.Any<IDictionary<string, IEnumerable<string>>>());
    }

    [Fact]
    public async Task SignInAsync_ShouldThrowUserBannedException_WhenUserIsBanned()
    {
        // Arrange
        var signInDto = new SignInDto
        {
            Email = "inactiveuser@example.com",
            Password = "password123"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = signInDto.Email,
            PasswordHash = new byte[32],
            PasswordSalt = new byte[32],
            IsActive = true,
            Banned = true
        };
        _userRepository.GetAsync(signInDto.Email.ToLowerInvariant()).Returns(user);
        _passwordManager.VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt).Returns(true);

        // Act & Assert
        await Should.ThrowAsync<UserBannedException>(() => _identityService.SignInAsync(signInDto));
        await _userRepository.Received(1).GetAsync(signInDto.Email.ToLowerInvariant());
        _passwordManager.Received(1).VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt);
        await _tokenManager.DidNotReceive().CreateTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg
                .Any<string>(),
            claims: Arg.Any<IDictionary<string, IEnumerable<string>>>());
    }

    #endregion

    #region ChangePasswordAsync

    [Fact]
    public async Task ChangePasswordAsync_ShouldChangePassword_WhenDataIsValid()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            UserId = Guid.NewGuid(),
            OldPassword = "OldPass123",
            NewPassword = "NewPass456"
        };

        var user = new User
        {
            Id = changePasswordDto.UserId,
            PasswordHash = new byte[32],
            PasswordSalt = new byte[32]
        };

        _userRepository.GetAsync(changePasswordDto.UserId).Returns(user);
        _passwordManager.VerifyPassword(changePasswordDto.OldPassword, user.PasswordHash, user.PasswordSalt)
            .Returns(true);

        // Act
        await _identityService.ChangePasswordAsync(changePasswordDto);

        // Assert
        _passwordManager.Received(1)
            .CreatePasswordHash(changePasswordDto.NewPassword, out Arg.Any<byte[]>(), out Arg.Any<byte[]>());
        await _userRepository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrowInvalidCredentialsException_WhenOldPasswordIsIncorrect()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            UserId = Guid.NewGuid(),
            OldPassword = "WrongPass",
            NewPassword = "NewPass456"
        };

        var user = new User
        {
            Id = changePasswordDto.UserId,
            PasswordHash = new byte[32],
            PasswordSalt = new byte[32]
        };

        _userRepository.GetAsync(changePasswordDto.UserId).Returns(user);
        _passwordManager.VerifyPassword(changePasswordDto.OldPassword, user.PasswordHash, user.PasswordSalt)
            .Returns(false);

        // Act & Assert
        await Should.ThrowAsync<InvalidCredentialsException>(() =>
            _identityService.ChangePasswordAsync(changePasswordDto));
        _passwordManager.Received(1)
            .VerifyPassword(changePasswordDto.OldPassword, user.PasswordHash, user.PasswordSalt);
        _passwordManager.DidNotReceive()
            .CreatePasswordHash(Arg.Any<string>(), out Arg.Any<byte[]>(), out Arg.Any<byte[]>());
        await _userRepository.DidNotReceive().UpdateAsync(user);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrowUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            UserId = Guid.NewGuid(),
            OldPassword = "OldPass123",
            NewPassword = "NewPass456"
        };

        var user = new User
        {
            Id = changePasswordDto.UserId,
            PasswordHash = new byte[32],
            PasswordSalt = new byte[32]
        };

        _userRepository.GetAsync(changePasswordDto.UserId).Returns((User)null!);

        // Act
        await Should.ThrowAsync<UserNotFoundException>(() => _identityService.ChangePasswordAsync(changePasswordDto));

        // Assert
        _passwordManager.Received(0)
            .CreatePasswordHash(changePasswordDto.NewPassword, out Arg.Any<byte[]>(), out Arg.Any<byte[]>());
        await _userRepository.Received(0).UpdateAsync(user);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrowPasswordRequirementsException_WhenPasswordIsWeak()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            UserId = Guid.NewGuid(),
            OldPassword = "OldPass123",
            NewPassword = "NewPass456"
        };

        var user = new User
        {
            Id = changePasswordDto.UserId,
            PasswordHash = new byte[32],
            PasswordSalt = new byte[32]
        };

        _userRepository.GetAsync(changePasswordDto.UserId).Returns(user);
        _passwordManager.VerifyPassword(changePasswordDto.OldPassword, user.PasswordHash, user.PasswordSalt)
            .Returns(true);
        _passwordManager.When(x => x.VerifyPasswordRequirements(Arg.Any<string>()))
            .Do(call => throw new PasswordRequirementsException(""));

        // Act
        await Should.ThrowAsync<PasswordRequirementsException>(() =>
            _identityService.ChangePasswordAsync(changePasswordDto));

        // Assert
        _passwordManager.Received(0)
            .CreatePasswordHash(changePasswordDto.NewPassword, out Arg.Any<byte[]>(), out Arg.Any<byte[]>());
        await _userRepository.Received(0).UpdateAsync(user);
    }

    #endregion

    #region RequestResetPasswordTokenAsync

    [Fact]
    public async Task RequestResetPasswordTokenAsync_ShouldGenerateResetToken_WhenUserExists()
    {
        // Arrange
        var requestResetPasswordTokenDto = new RequestResetPasswordTokenDto
        {
            Email = "test@example.com"
        };

        var user = new User { Email = requestResetPasswordTokenDto.Email };

        _userRepository.GetAsync(requestResetPasswordTokenDto.Email).Returns(user);

        // Act
        await _identityService.RequestResetPasswordTokenAsync(requestResetPasswordTokenDto);

        // Assert
        user.PasswordResetToken.ShouldNotBeNull();
        Assert.True(user.PasswordTokenExpires > _fakeTimeProvider.GetLocalNow().DateTime);
        await _userRepository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task RequestResetPasswordTokenAsync_ShouldThrowUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var requestResetPasswordTokenDto = new RequestResetPasswordTokenDto
        {
            Email = "nonexistent@example.com"
        };

        _userRepository.GetAsync(requestResetPasswordTokenDto.Email).Returns((User)null!);

        // Act & Assert
        await Should.ThrowAsync<UserNotFoundException>(() =>
            _identityService.RequestResetPasswordTokenAsync(requestResetPasswordTokenDto));
    }

    #endregion

    #region ResetPasswordAsync

    [Fact]
    public async Task ResetPasswordAsync_ShouldResetPassword_WhenResetTokenIsValid()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto
        {
            ResetToken = "valid-token",
            NewPassword = "NewPass456"
        };

        var user = new User
        {
            PasswordResetToken = resetPasswordDto.ResetToken,
            PasswordTokenExpires = _fakeTimeProvider.GetLocalNow().AddHours(1).DateTime
        };

        _userRepository.GetByResetPasswordToken(resetPasswordDto.ResetToken).Returns(user);

        // Act
        await _identityService.ResetPasswordAsync(resetPasswordDto);

        // Assert
        await _userRepository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrowPasswordRequirementsException_WhenPasswordIsWeak()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto
        {
            ResetToken = "valid-token",
            NewPassword = "NewPass456"
        };

        var user = new User
        {
            PasswordResetToken = resetPasswordDto.ResetToken,
            PasswordTokenExpires = _fakeTimeProvider.GetLocalNow().AddHours(1).DateTime
        };

        _userRepository.GetByResetPasswordToken(resetPasswordDto.ResetToken).Returns(user);
        _passwordManager.When(x => x.VerifyPasswordRequirements(Arg.Any<string>()))
            .Do(call => throw new PasswordRequirementsException(""));

        // Act
        await Should.ThrowAsync<PasswordRequirementsException>(() =>
            _identityService.ResetPasswordAsync(resetPasswordDto));

        // Assert
        await _userRepository.Received(0).UpdateAsync(user);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrowInvalidResetPasswordTokenException_WhenTokenIsInvalid()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto
        {
            ResetToken = "invalid-token",
            NewPassword = "NewPass456"
        };

        _userRepository.GetByResetPasswordToken(resetPasswordDto.ResetToken).Returns((User)null!);

        // Act & Assert
        await Should.ThrowAsync<InvalidResetPasswordTokenException>(() =>
            _identityService.ResetPasswordAsync(resetPasswordDto));
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrowResetPasswordTokenExpiredException_WhenTokenIsExpired()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto
        {
            ResetToken = "valid-token",
            NewPassword = "NewPass456"
        };

        var user = new User
        {
            PasswordResetToken = resetPasswordDto.ResetToken,
            PasswordTokenExpires = _fakeTimeProvider.GetLocalNow().AddHours(-1).DateTime
        };

        _userRepository.GetByResetPasswordToken(resetPasswordDto.ResetToken).Returns(user);

        // Act & Assert
        await Should.ThrowAsync<ResetPasswordTokenExpiredException>(() =>
            _identityService.ResetPasswordAsync(resetPasswordDto));
    }

    #endregion

    #region BanUserAsync

    [Fact]
    public async Task BanUserAsync_ShouldBanUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };

        _userRepository.GetAsync(userId).Returns(user);

        // Act
        await _identityService.BanUserAsync(userId);

        // Assert
        user.Banned.ShouldBeTrue();
        await _userRepository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task BanUserAsync_ShouldThrowUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetAsync(userId).Returns((User)null!);

        // Act & Assert
        await Should.ThrowAsync<UserNotFoundException>(() => _identityService.BanUserAsync(userId));
    }

    #endregion

    #region UnbanUserAsync

    [Fact]
    public async Task UnbanUserAsync_ShouldUnbanUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Banned = true };

        _userRepository.GetAsync(userId).Returns(user);

        // Act
        await _identityService.UnbanUserAsync(userId);

        // Assert
        user.Banned.ShouldBeFalse();
        await _userRepository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task UnbanUserAsync_ShouldThrowUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetAsync(userId).Returns((User)null!);

        // Act & Assert
        await Should.ThrowAsync<UserNotFoundException>(() => _identityService.UnbanUserAsync(userId));
    }

    #endregion

    #region RefreshTokenAsync

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewJwt_WhenValidRefreshTokenProvided()
    {
        // Arrange
        var refreshTokenDto = new RefreshTokenDto
        {
            Jwt = "some-jwt",
            RefreshToken = "valid-refresh-token"
        };

        var userId = Guid.NewGuid();
        var principal = Substitute.For<ClaimsPrincipal>();
        principal.Identity!.Name.Returns(userId.ToString());

        var user = new User { Id = userId, IsActive = true, Email = "john.doe@test.com", Role = "User", Claims = [] };

        _tokenManager.GetPrincipalFromToken(refreshTokenDto.Jwt).Returns(principal);
        _tokenManager.ValidRefreshAsync(userId.ToString(), refreshTokenDto.RefreshToken).Returns(Task.CompletedTask);
        _userRepository.GetAsync(userId).Returns(user);

        var newJwt = new JwtDto
        {
            AccessToken = new Token
            {
                GeneratedToken = "new-jwt-token",
                TokenExpires = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds()
            },
            RefreshToken = new Token
            {
                GeneratedToken = "new-refresh-token",
                TokenExpires = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds()
            }
        };

        _tokenManager.CreateTokenAsync(userId.ToString(), "john.doe@test.com", user.Role, claims: user.Claims)
            .Returns(newJwt);

        // Act
        var result = await _identityService.RefreshTokenAsync(refreshTokenDto);

        // Assert
        result.AccessToken.GeneratedToken.ShouldBe(newJwt.AccessToken.GeneratedToken);
        result.RefreshToken.GeneratedToken.ShouldBe(newJwt.RefreshToken.GeneratedToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowUserInvalidCredentialsException_WhenUserDoesNotExist()
    {
        // Arrange
        var refreshTokenDto = new RefreshTokenDto
        {
            Jwt = "some-jwt",
            RefreshToken = "valid-refresh-token"
        };

        var userId = Guid.NewGuid();
        var principal = Substitute.For<ClaimsPrincipal>();
        principal.Identity!.Name.Returns(userId.ToString());

        _tokenManager.GetPrincipalFromToken(refreshTokenDto.Jwt).Returns(principal);
        _tokenManager.ValidRefreshAsync(userId.ToString(), refreshTokenDto.RefreshToken).Returns(Task.CompletedTask);
        _userRepository.GetAsync(userId).Returns((User)null!);

        // Act & Assert
        await Should.ThrowAsync<InvalidCredentialsException>(() => _identityService.RefreshTokenAsync(refreshTokenDto));
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowInvalidRefreshTokenException_WhenTokenIsInvalid()
    {
        // Arrange
        var refreshTokenDto = new RefreshTokenDto
        {
            Jwt = "some-jwt",
            RefreshToken = "valid-refresh-token"
        };
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, IsActive = true, Email = "john.doe@test.com", Role = "User", Claims = [] };
        var principal = Substitute.For<ClaimsPrincipal>();
        principal.Identity!.Name.Returns(userId.ToString());

        _tokenManager.GetPrincipalFromToken(refreshTokenDto.Jwt).Returns(principal);
        _userRepository.GetAsync(userId).Returns(user);
        _tokenManager.When(x => x.ValidRefreshAsync(userId.ToString(), refreshTokenDto.RefreshToken))
            .Throw<InvalidRefreshTokenException>();

        // Act & Assert
        await Should.ThrowAsync<InvalidRefreshTokenException>(() =>
            _identityService.RefreshTokenAsync(refreshTokenDto));
    }

    #endregion

    #region NewActivationTokenAsync

    [Fact]
    public async Task NewActivationTokenAsync_ShouldGenerateAndSetNewToken_WhenUserExists()
    {
        // Arrange
        var newActivationTokenDto = new NewActivationTokenDto
        {
            Email = "existinguser@example.com"
        };

        var user = new User
        {
            Email = newActivationTokenDto.Email
        };

        var activationToken = new Token
        {
            GeneratedToken = "new-token",
            TokenExpires = new DateTimeOffset(_fakeTimeProvider.GetLocalNow().DateTime.AddDays(1))
                .ToUnixTimeMilliseconds()
        };

        _userRepository.GetAsync(newActivationTokenDto.Email).Returns(user);
        _tokenManager.GenerateActivationToken().Returns(activationToken);

        // Act
        await _identityService.NewActivationTokenAsync(newActivationTokenDto);

        // Assert
        user.ActivationToken.ShouldBe(activationToken.GeneratedToken);
        user.ActivationTokenExpires.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(activationToken.TokenExpires)
            .DateTime);
        await _userRepository.Received(1).GetAsync(newActivationTokenDto.Email);
        await _userRepository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task NewActivationTokenAsync_ShouldThrowUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var newActivationTokenDto = new NewActivationTokenDto
        {
            Email = "nonexistentuser@example.com"
        };

        _userRepository.GetAsync(newActivationTokenDto.Email).Returns((User)null!);

        // Act & Assert
        await Should.ThrowAsync<UserNotFoundException>(() =>
            _identityService.NewActivationTokenAsync(newActivationTokenDto));
        await _userRepository.Received(1).GetAsync(newActivationTokenDto.Email);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }

    #endregion

    #region ActivateAccountAsync

    [Fact]
    public async Task ActivateAccountAsync_ShouldActivateUser_WhenTokenIsValid()
    {
        // Arrange
        var activateAccountDto = new ActivateAccountDto
        {
            ActivationToken = "valid-token"
        };

        var user = new User
        {
            ActivationToken = activateAccountDto.ActivationToken,
            ActivationTokenExpires = _fakeTimeProvider.GetLocalNow().AddDays(1).DateTime,
            IsActive = false
        };

        _userRepository.GetByActivationToken(activateAccountDto.ActivationToken).Returns(user);

        // Act
        await _identityService.ActivateAccountAsync(activateAccountDto);

        // Assert
        user.IsActive.ShouldBeTrue();
        user.ActivationToken.ShouldBeNull();
        await _userRepository.Received(1).GetByActivationToken(activateAccountDto.ActivationToken);
        await _userRepository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ActivateAccountAsync_ShouldThrowInvalidActivationTokenException_WhenTokenIsInvalid()
    {
        // Arrange
        var activateAccountDto = new ActivateAccountDto
        {
            ActivationToken = "invalid-token"
        };

        _userRepository.GetByActivationToken(activateAccountDto.ActivationToken).Returns((User)null!);

        // Act & Assert
        await Should.ThrowAsync<InvalidActivationTokenException>(() =>
            _identityService.ActivateAccountAsync(activateAccountDto));
        await _userRepository.Received(1).GetByActivationToken(activateAccountDto.ActivationToken);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task ActivateAccountAsync_ShouldThrowActivationTokenExpiredException_WhenTokenIsExpired()
    {
        // Arrange
        var activateAccountDto = new ActivateAccountDto
        {
            ActivationToken = "expired-token"
        };

        var user = new User
        {
            ActivationToken = activateAccountDto.ActivationToken,
            ActivationTokenExpires = _fakeTimeProvider.GetLocalNow().AddDays(-1).DateTime // Token is expired
        };

        _userRepository.GetByActivationToken(activateAccountDto.ActivationToken).Returns(user);

        // Act & Assert
        await Should.ThrowAsync<ActivationTokenExpiredException>(() =>
            _identityService.ActivateAccountAsync(activateAccountDto));
        await _userRepository.Received(1).GetByActivationToken(activateAccountDto.ActivationToken);
        await _userRepository.DidNotReceive().UpdateAsync(user);
    }

    #endregion
}
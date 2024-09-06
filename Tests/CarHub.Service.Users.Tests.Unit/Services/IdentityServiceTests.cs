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
    private readonly IUserRepository _userRepository;
    private readonly IPasswordManager _passwordManager;
    private readonly ITokenManager _tokenManager;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IdentityService _identityService;

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
            Lastname = "Doe",
            Claims = []
        };
        _userRepository.GetAsync(signUpDto.Email.ToLowerInvariant()).Returns((User)null!);

        // Act
        await _identityService.SignUpAsync(signUpDto);

        // Assert
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email == signUpDto.Email &&
            u.Firstname == signUpDto.Firstname &&
            u.Lastname == signUpDto.Lastname &&
            u.IsActive == true));
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
            Lastname = "Doe",
            Claims = []
        };
        var existingUser = new User { Email = signUpDto.Email };
        _userRepository.GetAsync(signUpDto.Email.ToLowerInvariant()).Returns(existingUser);

        // Act & Assert
        await Should.ThrowAsync<EmailInUseException>(() => _identityService.SignUpAsync(signUpDto));
        await _userRepository.Received(1).GetAsync(signUpDto.Email.ToLowerInvariant());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>());
    }

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
        var jwt = new JwtDto("some-jwt-token", expires, user.Id.ToString(), user.Role, user.Email, user.Claims);

        _userRepository.GetAsync(signInDto.Email.ToLowerInvariant()).Returns(user);
        _passwordManager.VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt).Returns(true);
        _tokenManager.CreateToken(user.Id.ToString(), user.Email, user.Role, claims: user.Claims).Returns(jwt);

        // Act
        var result = await _identityService.SignInAsync(signInDto);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe(jwt.AccessToken);
        await _userRepository.Received(1).GetAsync(signInDto.Email.ToLowerInvariant());
        _passwordManager.Received(1).VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt);
        _tokenManager.Received(1).CreateToken(user.Id.ToString(), user.Email, user.Role, claims: user.Claims);
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
        _tokenManager.DidNotReceive().CreateToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
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
        _tokenManager.DidNotReceive().CreateToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
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
        _tokenManager.DidNotReceive().CreateToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            claims: Arg.Any<IDictionary<string, IEnumerable<string>>>());
    }
}
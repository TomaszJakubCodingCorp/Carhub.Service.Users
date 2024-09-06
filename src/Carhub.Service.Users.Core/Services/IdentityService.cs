using Carhub.Service.Users.Core.DTOs;
using Carhub.Service.Users.Core.Entities;
using Carhub.Service.Users.Core.Exceptions;
using Carhub.Service.Users.Core.Repositories;

namespace Carhub.Service.Users.Core.Services;

internal sealed class IdentityService(
    IUserRepository userRepository,
    IPasswordManager passwordManager,
    ITokenManager tokenManager,
    TimeProvider timeProvider)
    : IIdentityService
{
    public async Task<UserDto?> GetAsync(Guid id)
    {
        var user = await userRepository.GetAsync(id);

        return user is null
            ? null
            : new UserDto(user.Id, user.Email, user.Role, user.Firstname, user.Lastname,
                user.CreatedAt, user.Claims);
    }

    public async Task SignUpAsync(SignUpDto signUpDto)
    {
        var email = signUpDto.Email.ToLowerInvariant();
        var user = await userRepository.GetAsync(email);
        if (user is not null)
            throw new EmailInUseException(email);

        passwordManager.VerifyPasswordRequirements(signUpDto.Password);
        passwordManager.CreatePasswordHash(signUpDto.Password, out var hash, out var salt);

        user = new User
        {
            Id = signUpDto.Id,
            Email = signUpDto.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Firstname = signUpDto.Firstname,
            Lastname = signUpDto.Lastname,
            CreatedAt = timeProvider.GetLocalNow().DateTime,
            IsActive = true,
            Claims = signUpDto.Claims
        };

        await userRepository.AddAsync(user);
    }

    public async Task<JwtDto> SignInAsync(SignInDto signInDto)
    {
        var user = await userRepository.GetAsync(signInDto.Email.ToLowerInvariant())
                   ?? throw new InvalidCredentialsException();

        if (!passwordManager.VerifyPassword(signInDto.Password, user.PasswordHash, user.PasswordSalt))
            throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new UserNotActiveException(user.Id);

        var jwt = tokenManager.CreateToken(user.Id.ToString(), user.Email, user.Role, claims: user.Claims);

        return jwt;
    }
}
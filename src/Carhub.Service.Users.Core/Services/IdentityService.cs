using System.Security.Cryptography;
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
        var activationToken = tokenManager.GenerateActivationToken();

        user = new User
        {
            Id = signUpDto.Id,
            Email = signUpDto.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Firstname = signUpDto.Firstname,
            Lastname = signUpDto.Lastname,
            CreatedAt = timeProvider.GetLocalNow().DateTime,
            IsActive = false,
            Claims = [],
            Role = signUpDto.Role,
            Banned = false,
            ActivationToken = activationToken.GeneratedToken,
            ActivationTokenExpires = activationToken.DateTimeExpires()
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
            throw new UserNotActiveException(user.Email);

        if (user.Banned)
            throw new UserBannedException(user.Email);

        var jwt = await tokenManager.CreateTokenAsync(user.Id.ToString(), user.Email, user.Role, claims: user
            .Claims);
        jwt.Email = user.Email;

        return jwt;
    }

    public async Task ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        var user = await userRepository.GetAsync(changePasswordDto.UserId)
                   ?? throw new UserNotFoundException(changePasswordDto.UserId);
        if (!passwordManager.VerifyPassword(changePasswordDto.OldPassword, user.PasswordHash, user.PasswordSalt))
            throw new InvalidCredentialsException();

        passwordManager.VerifyPasswordRequirements(changePasswordDto.NewPassword);
        passwordManager.CreatePasswordHash(changePasswordDto.NewPassword, out var hash, out var salt);

        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await userRepository.UpdateAsync(user);
    }

    public async Task RequestResetPasswordTokenAsync(RequestResetPasswordTokenDto requestResetPasswordTokenDto)
    {
        var user = await userRepository.GetAsync(requestResetPasswordTokenDto.Email)
                   ?? throw new UserNotFoundException(requestResetPasswordTokenDto.Email);

        user.PasswordResetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(128));
        user.PasswordTokenExpires = timeProvider.GetLocalNow().AddDays(1).DateTime;
        await userRepository.UpdateAsync(user);

        //TODO: eventDispatcher.Send(UserRequestedResetPassword or something)
    }

    public async Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        var user = await userRepository.GetByResetPasswordToken(resetPasswordDto.ResetToken)
                   ?? throw new InvalidResetPasswordTokenException();

        if (user.PasswordTokenExpires < timeProvider.GetLocalNow().DateTime)
            throw new ResetPasswordTokenExpiredException();

        passwordManager.VerifyPasswordRequirements(resetPasswordDto.NewPassword);
        passwordManager.CreatePasswordHash(resetPasswordDto.NewPassword, out var hash, out var salt);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.PasswordResetToken = null;
        await userRepository.UpdateAsync(user);
    }

    public async Task BanUserAsync(Guid id)
    {
        var user = await userRepository.GetAsync(id)
                   ?? throw new UserNotFoundException(id);

        user.Banned = true;
        await userRepository.UpdateAsync(user);
    }

    public async Task UnbanUserAsync(Guid id)
    {
        var user = await userRepository.GetAsync(id)
                   ?? throw new UserNotFoundException(id);

        user.Banned = false;
        await userRepository.UpdateAsync(user);
    }

    public async Task<JwtDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        var principal = tokenManager.GetPrincipalFromToken(refreshTokenDto.Jwt);
        var userIdString = principal.Identity?.Name;

        if (!Guid.TryParse(userIdString, out var userId))
            throw new InvalidCredentialsException();

        await tokenManager.ValidRefreshAsync(userIdString, refreshTokenDto.RefreshToken);

        var user = await userRepository.GetAsync(userId)
                   ?? throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new UserNotActiveException(user.Email);

        var newJwt = await tokenManager.CreateTokenAsync(user.Id.ToString(), user.Email, user.Role, claims: user
            .Claims);
        newJwt.Email = user.Email;

        return newJwt;
    }

    public async Task NewActivationTokenAsync(NewActivationTokenDto newActivationTokenDto)
    {
        var user = await userRepository.GetAsync(newActivationTokenDto.Email)
                   ?? throw new UserNotFoundException(newActivationTokenDto.Email);

        var activationToken = tokenManager.GenerateActivationToken();

        user.ActivationToken = activationToken.GeneratedToken;
        user.ActivationTokenExpires = activationToken.DateTimeExpires();
        await userRepository.UpdateAsync(user);
    }

    public async Task ActivateAccountAsync(ActivateAccountDto activateAccountDto)
    {
        var user = await userRepository.GetByActivationToken(activateAccountDto.ActivationToken)
                   ?? throw new InvalidActivationTokenException();

        if (user.ActivationTokenExpires < timeProvider.GetLocalNow().DateTime)
            throw new ActivationTokenExpiredException();

        user.IsActive = true;
        user.ActivationToken = null;
        await userRepository.UpdateAsync(user);
    }
}
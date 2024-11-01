using Carhub.Service.Users.Core.DTOs;

namespace Carhub.Service.Users.Core.Services;

public interface IIdentityService
{
    Task<UserDto?> GetAsync(Guid id);
    Task SignUpAsync(SignUpDto signUpDto);
    Task<JwtDto> SignInAsync(SignInDto signInDto);
    Task ChangePasswordAsync(ChangePasswordDto changePasswordDto);
    Task RequestResetPasswordTokenAsync(RequestResetPasswordTokenDto requestResetPasswordTokenDto);
    Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task BanUserAsync(Guid id);
    Task UnbanUserAsync(Guid id);
    Task<JwtDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task NewActivationTokenAsync(NewActivationTokenDto newActivationTokenDto);
    Task ActivateAccountAsync(ActivateAccountDto activateAccountDto);
}
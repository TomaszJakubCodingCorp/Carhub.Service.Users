using Carhub.Service.Users.Core.DTOs;

namespace Carhub.Service.Users.Core.Services;

public interface IIdentityService
{
    Task<UserDto?> GetAsync(Guid id);
    Task SignUpAsync(SignUpDto signUpDto);
    Task<JwtDto> SignInAsync(SignInDto signInDto);
}
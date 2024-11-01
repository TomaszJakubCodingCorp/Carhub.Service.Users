namespace Carhub.Service.Users.Core.Services;

public interface IRefreshTokenService
{
    Task SaveRefreshTokenAsync(string userId, string refreshToken, TimeSpan expiryTime);
    Task<string?> GetRefreshTokenAsync(string userId);
}
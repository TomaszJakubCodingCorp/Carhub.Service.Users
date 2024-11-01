using StackExchange.Redis;

namespace Carhub.Service.Users.Core.Services;

internal sealed class RefreshTokenService(IConnectionMultiplexer redis) : IRefreshTokenService
{
    public async Task SaveRefreshTokenAsync(string userId, string refreshToken, TimeSpan expiryTime)
    {
        var db = redis.GetDatabase();
        var key = GetKey(userId);
        await db.StringSetAsync(key, refreshToken, expiryTime);
    }

    public async Task<string?> GetRefreshTokenAsync(string userId)
    {
        var db = redis.GetDatabase();
        var key = GetKey(userId);
        return await db.StringGetAsync(key);
    }

    private string GetKey(string userId)
    {
        return $"users.refreshToken:{userId}";
    }
}
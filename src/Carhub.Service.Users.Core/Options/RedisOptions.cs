namespace Carhub.Service.Users.Core.Options;

public class RedisOptions
{
    public const string OptionsName = nameof(RedisOptions);
    public string ConnectionString { get; set; } = string.Empty;
}
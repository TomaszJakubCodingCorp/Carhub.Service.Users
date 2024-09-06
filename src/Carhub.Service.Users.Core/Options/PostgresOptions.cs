namespace Carhub.Service.Users.Core.Options;

public class PostgresOptions
{
    public const string OptionsName = nameof(PostgresOptions);
    public string ConnectionString { get; set; } = string.Empty;
}
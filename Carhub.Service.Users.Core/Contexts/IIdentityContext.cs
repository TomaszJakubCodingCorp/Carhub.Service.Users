namespace Carhub.Service.Users.Core.Contexts;

public interface IIdentityContext
{
    bool IsAuthenticated { get; }
    Guid Id { get; }
    string Role { get; }
    Dictionary<string, IEnumerable<string>> Claims { get; }
}
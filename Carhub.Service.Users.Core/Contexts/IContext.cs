namespace Carhub.Service.Users.Core.Contexts;

public interface IContext
{
    string RequestId { get; }
    string TraceId { get; }
    IIdentityContext Identity { get; }
}
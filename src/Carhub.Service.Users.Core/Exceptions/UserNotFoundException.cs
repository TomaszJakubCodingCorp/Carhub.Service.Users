namespace Carhub.Service.Users.Core.Exceptions;

public sealed class UserNotFoundException : CarHubException
{
    public UserNotFoundException(Guid id) : base($"User with id: '{id:N}' was not found.")
    {
    }

    public UserNotFoundException(string email) : base($"User with email: '{email}' was not found.")
    {
    }
}
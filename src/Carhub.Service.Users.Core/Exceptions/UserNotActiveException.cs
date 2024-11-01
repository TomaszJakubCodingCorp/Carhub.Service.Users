namespace Carhub.Service.Users.Core.Exceptions;

public sealed class UserNotActiveException(string email)
    : CarHubException($"User with email : '{email}' is not active.");
namespace Carhub.Service.Users.Core.Exceptions;

public sealed class UserNotActiveException(Guid userId) : CarHubException($"User with ID : '{userId}' is not active.");
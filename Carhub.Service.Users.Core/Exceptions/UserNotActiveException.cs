namespace Carhub.Service.Users.Core.Exceptions;

public sealed class UserNotActiveException(Guid userId) : Exception($"User with ID : '{userId}' is not active.");
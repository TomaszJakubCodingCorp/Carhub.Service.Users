namespace Carhub.Service.Users.Core.Exceptions;

public sealed class UserBannedException(string email) : CarHubException($"User with email : '{email}' is banned.");
namespace Carhub.Service.Users.Core.Exceptions;

public sealed class EmailInUseException(string email) : CarHubException($"Email '{email}' is already in use.");
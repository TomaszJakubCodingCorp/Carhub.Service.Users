namespace Carhub.Service.Users.Core.Exceptions;

public sealed class EmailInUseException(string email) : Exception($"Email '{email}' is already in use.");
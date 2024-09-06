namespace Carhub.Service.Users.Core.Exceptions;

public sealed class PasswordRequirementsException(string message) : CarHubException(message);
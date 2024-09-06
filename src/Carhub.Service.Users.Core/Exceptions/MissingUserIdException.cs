namespace Carhub.Service.Users.Core.Exceptions;

public sealed class MissingUserIdException() : CarHubException("User ID claim (subject) cannot be empty");
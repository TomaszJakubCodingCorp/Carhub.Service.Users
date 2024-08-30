namespace Carhub.Service.Users.Core.Exceptions;

public sealed class MissingUserIdException() : Exception("User ID claim (subject) cannot be empty");
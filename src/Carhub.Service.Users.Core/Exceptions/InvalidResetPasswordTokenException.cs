namespace Carhub.Service.Users.Core.Exceptions;

public sealed class InvalidResetPasswordTokenException() : CarHubException("Given reset token is invalid.");
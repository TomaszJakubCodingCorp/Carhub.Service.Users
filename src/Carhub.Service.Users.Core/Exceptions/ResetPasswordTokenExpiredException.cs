namespace Carhub.Service.Users.Core.Exceptions;

public sealed class ResetPasswordTokenExpiredException() : CarHubException("Given reset token is expired.");
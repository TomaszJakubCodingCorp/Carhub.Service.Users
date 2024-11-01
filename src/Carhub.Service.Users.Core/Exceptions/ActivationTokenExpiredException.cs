namespace Carhub.Service.Users.Core.Exceptions;

public sealed class ActivationTokenExpiredException() : CarHubException("Given activation token is expired.");
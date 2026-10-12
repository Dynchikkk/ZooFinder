namespace ZooFinder.Application.Shared.ErrorHandling.Exceptions;

public sealed class UnauthorizedException(string message) : Exception(message);

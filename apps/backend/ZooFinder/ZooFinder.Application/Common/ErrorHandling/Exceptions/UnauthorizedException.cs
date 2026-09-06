namespace ZooFinder.Application.Common.ErrorHandling.Exceptions;

public sealed class UnauthorizedException(string message) : Exception(message);

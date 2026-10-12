namespace ZooFinder.Application.Shared.ErrorHandling.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message);

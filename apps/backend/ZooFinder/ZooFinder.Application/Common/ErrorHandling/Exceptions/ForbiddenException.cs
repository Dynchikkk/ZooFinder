namespace ZooFinder.Application.Common.ErrorHandling.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message);

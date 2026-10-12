namespace ZooFinder.Application.Shared.ErrorHandling.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);

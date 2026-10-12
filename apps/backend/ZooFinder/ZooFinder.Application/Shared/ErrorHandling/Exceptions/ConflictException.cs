namespace ZooFinder.Application.Shared.ErrorHandling.Exceptions;

public sealed class ConflictException(string message) : Exception(message);

namespace ZooFinder.Application.Common.ErrorHandling.Exceptions;

public sealed class ConflictException(string message) : Exception(message);

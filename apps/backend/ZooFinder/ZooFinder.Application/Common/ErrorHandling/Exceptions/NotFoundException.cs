namespace ZooFinder.Application.Common.ErrorHandling.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);

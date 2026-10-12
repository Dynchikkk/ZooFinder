namespace ZooFinder.Application.Shared.ErrorHandling.Exceptions;

public sealed class RequestValidationException(string message) : Exception(message);

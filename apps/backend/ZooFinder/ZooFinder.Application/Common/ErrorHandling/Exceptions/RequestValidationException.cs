namespace ZooFinder.Application.Common.ErrorHandling.Exceptions;

public sealed class RequestValidationException(string message) : Exception(message);

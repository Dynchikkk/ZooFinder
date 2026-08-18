namespace ZooFinder.Application.Common.Exceptions;

public sealed class RequestValidationException(string message) : Exception(message);

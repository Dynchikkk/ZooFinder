namespace ZooFinder.Application.Common.Animals.Exceptions;

public sealed class AnimalInformationUnavailableException : Exception
{
    public AnimalInformationUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

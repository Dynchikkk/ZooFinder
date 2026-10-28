namespace ZooFinder.Application.Modules.Animals.Modules.Recognition.Exceptions;

public sealed class AnimalRecognitionUnavailableException : Exception
{
    public AnimalRecognitionUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

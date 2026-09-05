using ZooFinder.Application.Common.Language.Constants;

namespace ZooFinder.Application.Features.Animals.Recognition.Contracts;

public sealed record AnimalRecognitionRequest(
    Stream ImageStream,
    string FileName,
    string ContentType,
    long Length,
    string LanguageCode = LanguageCodes.English);

using ZooFinder.Application.Shared.Language.Constants;

namespace ZooFinder.Application.Common.Animals.Services.AnimalRegistration;

public sealed record AnimalRegistrationResult(
    Guid AnimalId,
    Guid GeneralRoomId);

public sealed record RegisterAnimalRequest(
    string InformationSource,
    string SourceItemId,
    string LanguageCode = LanguageCodes.English);

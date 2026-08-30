using ZooFinder.Application.Common.Constants;

namespace ZooFinder.Application.Features.Animals.Catalog.Contracts;

public sealed record AnimalPageRequest(
    string InformationSource,
    string SourceItemId,
    string LanguageCode = LanguageCodes.English);

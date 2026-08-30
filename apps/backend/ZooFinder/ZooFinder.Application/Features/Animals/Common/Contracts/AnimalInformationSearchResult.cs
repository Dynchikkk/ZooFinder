namespace ZooFinder.Application.Features.Animals.Common.Contracts;

public sealed record AnimalInformationSearchResult(
    string InformationSource,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ImageUrl);

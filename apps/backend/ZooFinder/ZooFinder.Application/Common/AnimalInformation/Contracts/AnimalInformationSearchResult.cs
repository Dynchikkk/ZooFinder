namespace ZooFinder.Application.Common.AnimalInformation.Contracts;

public sealed record AnimalInformationSearchResult(
    string InformationSource,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ImageUrl);

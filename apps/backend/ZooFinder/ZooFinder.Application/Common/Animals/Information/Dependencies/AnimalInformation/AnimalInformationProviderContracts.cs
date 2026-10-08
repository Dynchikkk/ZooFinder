namespace ZooFinder.Application.Common.Animals.Information.Dependencies.AnimalInformation;

public sealed record AnimalInformationDetailsResult(
    string InformationSource,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ShortDescription,
    string? ImageUrl,
    string SourceUrl);

public sealed record AnimalInformationSearchResult(
    string InformationSource,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ImageUrl);

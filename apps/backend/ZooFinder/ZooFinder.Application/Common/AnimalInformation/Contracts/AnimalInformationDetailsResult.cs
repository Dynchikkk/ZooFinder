namespace ZooFinder.Application.Common.AnimalInformation.Contracts;

public sealed record AnimalInformationDetailsResult(
    string InformationSource,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ShortDescription,
    string? ImageUrl,
    string SourceUrl);

namespace ZooFinder.Application.Features.Animals.Common.Contracts;

public sealed record AnimalInformationSearchItem(
    string Source,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ShortDescription,
    string? ImageUrl,
    string SourceUrl);

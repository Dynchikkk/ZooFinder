namespace ZooFinder.Application.Features.Animals.Search.Contracts;

public sealed record AnimalSearchItemResponse(
    string Source,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ShortDescription,
    string? ImageUrl,
    string SourceUrl);

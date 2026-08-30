namespace ZooFinder.Application.Features.Animals.Catalog.Contracts;

public sealed record AnimalCardResponse(
    Guid? LocalAnimalId,
    string InformationSource,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ImageUrl);

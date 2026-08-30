namespace ZooFinder.Application.Features.Animals.Catalog.Contracts;

public sealed record AnimalPageResponse(
    Guid? LocalAnimalId,
    string InformationSource,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ShortDescription,
    string? ImageUrl,
    string SourceUrl)
{
    public bool HasStartedDiscussion => LocalAnimalId.HasValue;
}

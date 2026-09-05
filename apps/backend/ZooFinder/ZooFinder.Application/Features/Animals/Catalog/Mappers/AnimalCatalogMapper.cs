using ZooFinder.Application.Common.AnimalInformation.Contracts;
using ZooFinder.Application.Features.Animals.Catalog.Contracts;
using ZooFinder.Domain.Animals;

namespace ZooFinder.Application.Features.Animals.Catalog.Mappers;

public static class AnimalCatalogMapper
{
    public static AnimalCardResponse ToCardResponse(AnimalInformationSearchResult result)
    {
        return new AnimalCardResponse(
            null,
            result.InformationSource,
            result.SourceItemId,
            result.LanguageCode,
            result.Title,
            result.ScientificName,
            result.ImageUrl);
    }

    public static AnimalCardResponse ToCardResponse(Animal animal)
    {
        return new AnimalCardResponse(
            animal.Id,
            animal.InformationSource,
            animal.SourceItemId,
            animal.LanguageCode,
            animal.Title,
            animal.ScientificName,
            animal.ImageUrl);
    }

    public static AnimalPageResponse ToPageResponse(
        AnimalInformationDetailsResult result,
        Guid? localAnimalId)
    {
        return new AnimalPageResponse(
            localAnimalId,
            result.InformationSource,
            result.SourceItemId,
            result.LanguageCode,
            result.Title,
            result.ScientificName,
            result.ShortDescription,
            result.ImageUrl,
            result.SourceUrl);
    }
}

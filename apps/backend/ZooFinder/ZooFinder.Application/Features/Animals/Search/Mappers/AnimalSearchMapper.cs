using ZooFinder.Application.Features.Animals.Common.Contracts;
using ZooFinder.Application.Features.Animals.Search.Contracts;

namespace ZooFinder.Application.Features.Animals.Search.Mappers;

public static class AnimalSearchMapper
{
    public static AnimalSearchItemResponse ToResponse(AnimalInformationSearchItem item)
    {
        return new AnimalSearchItemResponse(
            item.Source,
            item.SourceItemId,
            item.LanguageCode,
            item.Title,
            item.ScientificName,
            item.ShortDescription,
            item.ImageUrl,
            item.SourceUrl);
    }
}

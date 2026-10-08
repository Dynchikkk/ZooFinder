using ZooFinder.Application.Common.Language.Constants;
using ZooFinder.Application.Common.Pagination.Constants;

namespace ZooFinder.Application.Features.Parks.Animals.Services.ParkAnimals;

public sealed record AddParkAnimalRequest(
    Guid ParkId, string InformationSource, string SourceItemId,
    string LanguageCode = LanguageCodes.English, string? LocalDescription = null, bool IsPublished = true);

public sealed record ParkAnimalResponse(
    Guid Id, Guid ParkId, Guid AnimalId, string Title, string? ScientificName,
    string? ShortDescription, string? ImageUrl, string? SourceUrl,
    string? LocalDescription, bool IsPublished, string InformationSource, string SourceItemId, string LanguageCode);

public sealed record ParkAnimalSearchRequest(Guid ParkId, bool IncludeUnpublished = false,
    int Page = 0, int PageSize = PaginationDefaults.DefaultPageSize);

public sealed record UpdateParkAnimalRequest(Guid ParkAnimalId, string? LocalDescription, bool IsPublished);

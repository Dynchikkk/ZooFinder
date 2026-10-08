using ZooFinder.Application.Common.Language.Constants;
using ZooFinder.Application.Common.Pagination.Constants;

namespace ZooFinder.Application.Features.Animals.Catalog.Services.AnimalCatalog;

public sealed record AnimalCardResponse(
    Guid? LocalAnimalId,
    string InformationSource,
    string SourceItemId,
    string LanguageCode,
    string Title,
    string? ScientificName,
    string? ImageUrl);

public sealed record AnimalPageRequest(
    string InformationSource,
    string SourceItemId,
    string LanguageCode = LanguageCodes.English);

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

public sealed record AnimalSearchRequest(
    string SearchTerm,
    AnimalSearchScope Scope = AnimalSearchScope.ExternalCatalog,
    string LanguageCode = LanguageCodes.English,
    string? Cursor = null,
    int Limit = PaginationDefaults.DefaultPageSize);

public enum AnimalSearchScope
{
    None = 0,
    ExternalCatalog = 1,
    LocalCatalog = 2
}

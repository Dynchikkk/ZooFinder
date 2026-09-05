using ZooFinder.Application.Common.Language.Constants;
using ZooFinder.Application.Common.Pagination.Contracts;

namespace ZooFinder.Application.Features.Animals.Catalog.Contracts;

public sealed record AnimalSearchRequest(
    string SearchTerm,
    AnimalSearchScope Scope = AnimalSearchScope.ExternalCatalog,
    string LanguageCode = LanguageCodes.English,
    string? Cursor = null,
    int Limit = PaginationDefaults.DefaultPageSize);

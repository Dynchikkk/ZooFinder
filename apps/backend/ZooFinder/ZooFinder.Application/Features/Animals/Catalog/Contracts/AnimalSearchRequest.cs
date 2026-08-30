using ZooFinder.Application.Common.Constants;
using ZooFinder.Application.Common.Contracts.Pagination;
using ZooFinder.Application.Features.Animals.Common.Contracts;

namespace ZooFinder.Application.Features.Animals.Catalog.Contracts;

public sealed record AnimalSearchRequest(
    string SearchTerm,
    AnimalSearchScope Scope = AnimalSearchScope.ExternalCatalog,
    string LanguageCode = LanguageCodes.English,
    string? Cursor = null,
    int Limit = PaginationDefaults.DefaultPageSize);

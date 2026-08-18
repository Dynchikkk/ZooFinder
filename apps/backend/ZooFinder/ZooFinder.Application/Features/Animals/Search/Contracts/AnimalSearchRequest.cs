using ZooFinder.Application.Common.Contracts.Pagination;
using ZooFinder.Application.Common.Constants;

namespace ZooFinder.Application.Features.Animals.Search.Contracts;

public sealed record AnimalSearchRequest(
    string SearchTerm,
    string LanguageCode = LanguageCodes.English,
    string? Cursor = null,
    int Limit = PaginationDefaults.DefaultPageSize);

using ZooFinder.Application.Common.Contracts.Pagination;
using ZooFinder.Application.Common.Constants;
using ZooFinder.Application.Common.Exceptions;
using ZooFinder.Application.Common.Extensions;
using ZooFinder.Application.Features.Animals.Common.Interfaces;
using ZooFinder.Application.Features.Animals.Search.Contracts;
using ZooFinder.Application.Features.Animals.Search.Interfaces;
using ZooFinder.Application.Features.Animals.Search.Mappers;

namespace ZooFinder.Application.Features.Animals.Search.Services;

public sealed class AnimalSearchService(IAnimalInformationService animalInformationService) : IAnimalSearchService
{
    private const int MinimumSearchTermLength = 2;
    private const int MaximumSearchTermLength = 200;

    public async Task<CursorPageResponse<AnimalSearchItemResponse>> SearchAsync(AnimalSearchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string searchTerm = request.SearchTerm?.Trim() ?? string.Empty;
        string languageCode = request.LanguageCode?.Trim().ToLowerInvariant() ?? string.Empty;

        Validate(searchTerm, languageCode, request.Limit);

        var pageRequest = new CursorPageRequest(request.Cursor.NormalizeCursor(), request.Limit);
        var searchResult = await animalInformationService.SearchAsync(
            searchTerm,
            languageCode,
            pageRequest,
            cancellationToken);

        return new CursorPageResponse<AnimalSearchItemResponse>([.. searchResult.Items.Select(AnimalSearchMapper.ToResponse)], searchResult.NextCursor);
    }

    private static void Validate(string searchTerm, string languageCode, int limit)
    {
        if (searchTerm.Length < MinimumSearchTermLength || searchTerm.Length > MaximumSearchTermLength)
        {
            throw new RequestValidationException(
                $"Search term length must be between {MinimumSearchTermLength} and {MaximumSearchTermLength} characters.");
        }

        if (!LanguageCodes.IsSupported(languageCode))
        {
            throw new RequestValidationException(
                $"Language code must be '{LanguageCodes.English}' or '{LanguageCodes.Russian}'.");
        }

        if (limit < 1 || limit > PaginationDefaults.MaximumPageSize)
        {
            throw new RequestValidationException(
                $"Page size must be between 1 and {PaginationDefaults.MaximumPageSize}.");
        }
    }
}

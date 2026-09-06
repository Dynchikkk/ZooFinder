using ZooFinder.Application.Common.AnimalInformation.Extensions;
using ZooFinder.Application.Common.AnimalInformation.Interfaces;
using ZooFinder.Application.Common.AnimalInformation.Validators;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Language.Validators;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Common.Pagination.Extensions;
using ZooFinder.Application.Features.Animals.Catalog.Contracts;
using ZooFinder.Application.Features.Animals.Catalog.Interfaces;

namespace ZooFinder.Application.Features.Animals.Catalog.Services;

public sealed class AnimalCatalogService : IAnimalCatalogService
{
    private const int MinimumSearchTermLength = 2;
    private const int MaximumSearchTermLength = 200;

    private readonly IAnimalInformationProvider _animalInformationProvider;
    private readonly IAnimalCatalogRepository _animalCatalogRepository;

    public AnimalCatalogService(
        IAnimalInformationProvider animalInformationProvider,
        IAnimalCatalogRepository animalCatalogRepository)
    {
        _animalInformationProvider = animalInformationProvider;
        _animalCatalogRepository = animalCatalogRepository;
    }

    public async Task<CursorPageResponse<AnimalCardResponse>> SearchAsync(
        AnimalSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string searchTerm = request.SearchTerm?.Trim() ?? string.Empty;
        string languageCode = request.LanguageCode.NormalizeLanguageCode();

        ValidateSearchRequest(searchTerm, languageCode, request.Limit);

        var pageRequest = new CursorPageRequest(request.Cursor.NormalizeCursor(), request.Limit);

        return request.Scope switch
        {
            AnimalSearchScope.LocalCatalog => await SearchLocalCatalogAsync(
                searchTerm,
                languageCode,
                pageRequest,
                cancellationToken),
            AnimalSearchScope.ExternalCatalog => await SearchExternalCatalogAsync(
                searchTerm,
                languageCode,
                pageRequest,
                cancellationToken),
            _ => throw new RequestValidationException("Animal search scope is not supported.")
        };
    }

    public async Task<AnimalPageResponse> GetAnimalAsync(
        AnimalPageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string informationSource = request.InformationSource.NormalizeInformationSource();
        string sourceItemId = request.SourceItemId.NormalizeSourceItemId();
        string languageCode = request.LanguageCode.NormalizeLanguageCode();

        AnimalInformationValidator.ValidateIdentity(
            informationSource,
            sourceItemId,
            languageCode);

        var information = await _animalInformationProvider.GetDetailsAsync(
            informationSource,
            sourceItemId,
            languageCode,
            cancellationToken)
            ?? throw new NotFoundException("Animal information was not found.");

        var localAnimal = await _animalCatalogRepository.GetBySourceItemAsync(
            informationSource,
            sourceItemId,
            languageCode,
            cancellationToken);

        return new AnimalPageResponse(
            localAnimal?.Id,
            information.InformationSource,
            information.SourceItemId,
            information.LanguageCode,
            information.Title,
            information.ScientificName,
            information.ShortDescription,
            information.ImageUrl,
            information.SourceUrl);
    }

    private async Task<CursorPageResponse<AnimalCardResponse>> SearchLocalCatalogAsync(
        string searchTerm,
        string languageCode,
        CursorPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var page = await _animalCatalogRepository.SearchAsync(
            searchTerm,
            languageCode,
            pageRequest,
            cancellationToken);

        AnimalCardResponse[] items = page.Items
            .Select(animal => new AnimalCardResponse(
                animal.Id,
                animal.InformationSource,
                animal.SourceItemId,
                animal.LanguageCode,
                animal.Title,
                animal.ScientificName,
                animal.ImageUrl))
            .ToArray();

        return new CursorPageResponse<AnimalCardResponse>(items, page.NextCursor);
    }

    private async Task<CursorPageResponse<AnimalCardResponse>> SearchExternalCatalogAsync(
        string searchTerm,
        string languageCode,
        CursorPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var page = await _animalInformationProvider.SearchAsync(
            searchTerm,
            languageCode,
            pageRequest,
            cancellationToken);

        AnimalCardResponse[] items = page.Items
            .Select(result => new AnimalCardResponse(
                null,
                result.InformationSource,
                result.SourceItemId,
                result.LanguageCode,
                result.Title,
                result.ScientificName,
                result.ImageUrl))
            .ToArray();

        return new CursorPageResponse<AnimalCardResponse>(items, page.NextCursor);
    }

    private static void ValidateSearchRequest(
        string searchTerm,
        string languageCode,
        int limit)
    {
        if (searchTerm.Length < MinimumSearchTermLength || searchTerm.Length > MaximumSearchTermLength)
        {
            throw new RequestValidationException(
                $"Search term length must be between {MinimumSearchTermLength} and {MaximumSearchTermLength} characters.");
        }

        LanguageCodeValidator.Validate(languageCode);

        if (limit < 1 || limit > PaginationDefaults.MaximumPageSize)
        {
            throw new RequestValidationException(
                $"Page size must be between 1 and {PaginationDefaults.MaximumPageSize}.");
        }
    }

}

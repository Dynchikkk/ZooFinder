using ZooFinder.Application.Common.Contracts.Pagination;
using ZooFinder.Application.Common.Exceptions;
using ZooFinder.Application.Common.Extensions;
using ZooFinder.Application.Features.Animals.Catalog.Contracts;
using ZooFinder.Application.Features.Animals.Catalog.Interfaces;
using ZooFinder.Application.Features.Animals.Catalog.Mappers;
using ZooFinder.Application.Features.Animals.Common.Contracts;
using ZooFinder.Application.Features.Animals.Common.Extensions;
using ZooFinder.Application.Features.Animals.Common.Interfaces;
using ZooFinder.Application.Features.Animals.Common.Validators;

namespace ZooFinder.Application.Features.Animals.Catalog.Services;

public sealed class AnimalCatalogService : IAnimalCatalogService
{
    private const int MinimumSearchTermLength = 2;
    private const int MaximumSearchTermLength = 200;
    private const int MaximumInformationSourceLength = 100;
    private const int MaximumSourceItemIdLength = 500;

    private readonly IAnimalInformationService _animalInformationService;
    private readonly IAnimalRepository _animalRepository;

    public AnimalCatalogService(
        IAnimalInformationService animalInformationService,
        IAnimalRepository animalRepository)
    {
        _animalInformationService = animalInformationService;
        _animalRepository = animalRepository;
    }

    public async Task<CursorPageResponse<AnimalCardResponse>> SearchAsync(
        AnimalSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string searchTerm = request.SearchTerm?.Trim() ?? string.Empty;
        string languageCode = request.LanguageCode.NormalizeLanguageCode();

        ValidateSearchRequest(searchTerm, languageCode, request.Scope, request.Limit);

        var pageRequest = new CursorPageRequest(request.Cursor.NormalizeCursor(), request.Limit);

        if (request.Scope == AnimalSearchScope.LocalCatalog)
        {
            var localPage = await _animalRepository.SearchAsync(
                searchTerm,
                languageCode,
                pageRequest,
                cancellationToken);

            return new CursorPageResponse<AnimalCardResponse>(
                [.. localPage.Items.Select(AnimalCatalogMapper.ToCardResponse)],
                localPage.NextCursor);
        }

        var externalPage = await _animalInformationService.SearchAsync(
            searchTerm,
            languageCode,
            pageRequest,
            cancellationToken);

        return new CursorPageResponse<AnimalCardResponse>(
            [.. externalPage.Items.Select(AnimalCatalogMapper.ToCardResponse)],
            externalPage.NextCursor);
    }

    public async Task<AnimalPageResponse> GetAnimalAsync(
        AnimalPageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string informationSource = request.InformationSource.NormalizeInformationSource();
        string sourceItemId = request.SourceItemId.NormalizeSourceItemId();
        string languageCode = request.LanguageCode.NormalizeLanguageCode();

        ValidateAnimalIdentity(informationSource, sourceItemId, languageCode);

        var information = await _animalInformationService.GetDetailsAsync(
            informationSource,
            sourceItemId,
            languageCode,
            cancellationToken);

        if (information == null)
        {
            throw new NotFoundException("Animal information was not found.");
        }

        var localAnimal = await _animalRepository.GetBySourceItemAsync(
            informationSource,
            sourceItemId,
            languageCode,
            cancellationToken);

        return AnimalCatalogMapper.ToPageResponse(information, localAnimal?.Id);
    }

    private static void ValidateSearchRequest(
        string searchTerm,
        string languageCode,
        AnimalSearchScope scope,
        int limit)
    {
        if (searchTerm.Length < MinimumSearchTermLength || searchTerm.Length > MaximumSearchTermLength)
        {
            throw new RequestValidationException(
                $"Search term length must be between {MinimumSearchTermLength} and {MaximumSearchTermLength} characters.");
        }

        if (scope != AnimalSearchScope.ExternalCatalog && scope != AnimalSearchScope.LocalCatalog)
        {
            throw new RequestValidationException("Animal search scope is not supported.");
        }

        AnimalInformationValidator.ValidateLanguageCode(languageCode);

        if (limit < 1 || limit > PaginationDefaults.MaximumPageSize)
        {
            throw new RequestValidationException(
                $"Page size must be between 1 and {PaginationDefaults.MaximumPageSize}.");
        }
    }

    private static void ValidateAnimalIdentity(
        string informationSource,
        string sourceItemId,
        string languageCode)
    {
        if (informationSource.Length == 0 || informationSource.Length > MaximumInformationSourceLength)
        {
            throw new RequestValidationException(
                $"Information source length must be between 1 and {MaximumInformationSourceLength} characters.");
        }

        if (sourceItemId.Length == 0 || sourceItemId.Length > MaximumSourceItemIdLength)
        {
            throw new RequestValidationException(
                $"Source item ID length must be between 1 and {MaximumSourceItemIdLength} characters.");
        }

        AnimalInformationValidator.ValidateLanguageCode(languageCode);
    }
}

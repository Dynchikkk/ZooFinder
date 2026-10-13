using ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;
using ZooFinder.Application.Common.Animals.Exceptions;
using ZooFinder.Application.Common.Animals.Extensions;
using ZooFinder.Application.Common.Animals.Services.AnimalRegistration;
using ZooFinder.Application.Common.Animals.Validators;
using ZooFinder.Application.Modules.Parks.DataSources;
using ZooFinder.Application.Modules.Parks.Extensions;
using ZooFinder.Application.Modules.Parks.Modules.Import.Validators;
using ZooFinder.Application.Modules.Parks.Validators;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Language.Extensions;
using ZooFinder.Application.Shared.Language.Validators;
using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Modules.Parks.Modules.Import.Services.ParkAnimalImport;

public sealed class ParkAnimalImportService : IParkAnimalImportService
{
    private const int SearchPageSize = 20;

    private readonly IParkCatalogDataSource _parkCatalogDataSource;
    private readonly IParkAnimalDataSource _parkAnimalDataSource;
    private readonly IAnimalInformationProvider _animalInformationProvider;
    private readonly IAnimalRegistrationService _animalRegistrationService;

    public ParkAnimalImportService(
        IParkCatalogDataSource parkCatalogDataSource,
        IParkAnimalDataSource parkAnimalDataSource,
        IAnimalInformationProvider animalInformationProvider,
        IAnimalRegistrationService animalRegistrationService)
    {
        ArgumentNullException.ThrowIfNull(parkCatalogDataSource);
        ArgumentNullException.ThrowIfNull(parkAnimalDataSource);
        ArgumentNullException.ThrowIfNull(animalInformationProvider);
        ArgumentNullException.ThrowIfNull(animalRegistrationService);

        _parkCatalogDataSource = parkCatalogDataSource;
        _parkAnimalDataSource = parkAnimalDataSource;
        _animalInformationProvider = animalInformationProvider;
        _animalRegistrationService = animalRegistrationService;
    }

    public async Task<ParkAnimalImportPreviewResponse> PreviewAsync(
        PreviewParkAnimalImportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkImportValidator.ValidateRows(request.Rows);
        await ValidateParkAsync(request.ParkId, cancellationToken);

        var results = new List<ParkAnimalImportRowResponse>(request.Rows.Count);
        var identities = new HashSet<(string Source, string ItemId, string Language)>();

        for (int rowIndex = 0; rowIndex < request.Rows.Count; rowIndex++)
        {
            var result = await PreviewRowAsync(rowIndex, request.Rows[rowIndex], identities, cancellationToken);
            results.Add(result);
        }

        return new ParkAnimalImportPreviewResponse(results);
    }

    public async Task<ParkAnimalImportResponse> ApplyAsync(
        ApplyParkAnimalImportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkImportValidator.ValidateRows(request.Rows);
        await ValidateParkAsync(request.ParkId, cancellationToken);

        var animalInformation = await PrepareAnimalInformationAsync(request.Rows, cancellationToken);
        var parkAnimals = new List<ParkAnimal>(request.Rows.Count);
        var registeredAnimalIds = new HashSet<Guid>();

        for (int rowIndex = 0; rowIndex < request.Rows.Count; rowIndex++)
        {
            ParkAnimalImportSelection selection = request.Rows[rowIndex];
            var registration = await _animalRegistrationService.RegisterAsync(animalInformation[rowIndex], cancellationToken);

            if (!registeredAnimalIds.Add(registration.AnimalId))
            {
                throw new RequestValidationException("Import rows resolve to the same species.");
            }

            parkAnimals.Add(new ParkAnimal
            {
                Id = Guid.NewGuid(),
                ParkId = request.ParkId,
                AnimalId = registration.AnimalId,
                LocalDescription = selection.LocalDescription.NormalizeOptionalText(),
                IsPublished = selection.IsPublished
            });
        }

        var savedParkAnimals = await _parkAnimalDataSource.UpsertBatchAsync(request.ParkId, parkAnimals, cancellationToken);
        var parkAnimalIds = savedParkAnimals
            .Select(parkAnimal => parkAnimal.Id)
            .ToArray();

        return new ParkAnimalImportResponse(parkAnimalIds);
    }

    private async Task<ParkAnimalImportRowResponse> PreviewRowAsync(
        int rowIndex,
        ParkAnimalImportRow row,
        HashSet<(string Source, string ItemId, string Language)> identities,
        CancellationToken cancellationToken)
    {
        try
        {
            string languageCode = row.LanguageCode.NormalizeLanguageCode();
            string? localDescription = row.LocalDescription.NormalizeOptionalText();
            ParkAnimalValidator.ValidateDescription(localDescription);

            var information = await ResolveAnimalInformationAsync(row, languageCode, cancellationToken);
            var identity = (
                Source: information.InformationSource.NormalizeInformationSource(),
                ItemId: information.SourceItemId.NormalizeSourceItemId(),
                Language: information.LanguageCode.NormalizeLanguageCode());

            if (!identities.Add(identity))
            {
                return new ParkAnimalImportRowResponse(rowIndex, null, "Duplicate animal in import.");
            }

            var match = new ParkAnimalImportMatchResponse(
                InformationSource: information.InformationSource,
                SourceItemId: information.SourceItemId,
                LanguageCode: information.LanguageCode,
                Title: information.Title,
                ScientificName: information.ScientificName,
                LocalDescription: localDescription);

            return new ParkAnimalImportRowResponse(rowIndex, match, null);
        }
        catch (RequestValidationException exception)
        {
            return new ParkAnimalImportRowResponse(rowIndex, null, exception.Message);
        }
        catch (NotFoundException exception)
        {
            return new ParkAnimalImportRowResponse(rowIndex, null, exception.Message);
        }
        catch (AnimalInformationUnavailableException exception)
        {
            return new ParkAnimalImportRowResponse(rowIndex, null, exception.Message);
        }
    }

    private async Task<IReadOnlyList<AnimalInformationDetailsResult>> PrepareAnimalInformationAsync(
        IReadOnlyList<ParkAnimalImportSelection> selections,
        CancellationToken cancellationToken)
    {
        var animalInformation = new List<AnimalInformationDetailsResult>(selections.Count);
        var identities = new HashSet<(string Source, string ItemId, string Language)>();

        foreach (ParkAnimalImportSelection selection in selections)
        {
            string informationSource = selection.InformationSource.NormalizeInformationSource();
            string sourceItemId = selection.SourceItemId.NormalizeSourceItemId();
            string languageCode = selection.LanguageCode.NormalizeLanguageCode();

            AnimalInformationValidator.ValidateIdentity(informationSource, sourceItemId, languageCode);
            ParkAnimalValidator.ValidateDescription(selection.LocalDescription.NormalizeOptionalText());

            if (!identities.Add((informationSource, sourceItemId, languageCode)))
            {
                throw new RequestValidationException("Duplicate animal in import.");
            }

            var information = await _animalInformationProvider.GetDetailsAsync(
                informationSource, sourceItemId, languageCode, cancellationToken)
                ?? throw new NotFoundException("An import animal was not found.");

            ValidateAnimalInformation(information, informationSource, sourceItemId, languageCode);
            animalInformation.Add(information);
        }

        return animalInformation;
    }

    private async Task<AnimalInformationDetailsResult> ResolveAnimalInformationAsync(
        ParkAnimalImportRow row,
        string languageCode,
        CancellationToken cancellationToken)
    {
        string informationSource = row.InformationSource.NormalizeInformationSource();
        string sourceItemId = row.SourceItemId.NormalizeSourceItemId();
        string commonName = row.CommonName.NormalizeText();
        string scientificName = row.ScientificName.NormalizeText();

        ParkImportValidator.ValidateName(commonName);
        ParkImportValidator.ValidateName(scientificName);

        if (informationSource.Length > 0 || sourceItemId.Length > 0)
        {
            AnimalInformationValidator.ValidateIdentity(informationSource, sourceItemId, languageCode);
        }
        else
        {
            LanguageCodeValidator.Validate(languageCode);

            string searchTerm = scientificName.Length > 0 ? scientificName : commonName;

            if (searchTerm.Length == 0)
            {
                throw new RequestValidationException("Import row needs an animal name or source identity.");
            }

            var pageRequest = new CursorPageRequest(null, SearchPageSize);
            var page = await _animalInformationProvider.SearchAsync(searchTerm, languageCode, pageRequest, cancellationToken);
            var matches = page.Items
                .Where(candidate => scientificName.Length > 0
                    ? string.Equals(candidate.ScientificName?.Trim(), scientificName, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(candidate.Title.Trim(), commonName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length != 1 || page.NextCursor != null)
            {
                throw new RequestValidationException("Animal match is missing or ambiguous; select a source item explicitly.");
            }

            informationSource = matches[0].InformationSource;
            sourceItemId = matches[0].SourceItemId;
        }

        var information = await _animalInformationProvider.GetDetailsAsync(
            informationSource, sourceItemId, languageCode, cancellationToken)
            ?? throw new NotFoundException("Import animal was not found.");

        ValidateAnimalInformation(information, informationSource, sourceItemId, languageCode);
        return information;
    }

    private async Task ValidateParkAsync(Guid parkId, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(parkId);

        if (await _parkCatalogDataSource.GetAsync(parkId, cancellationToken) == null)
        {
            throw new NotFoundException("Park was not found.");
        }
    }

    private static void ValidateAnimalInformation(
        AnimalInformationDetailsResult information,
        string informationSource,
        string sourceItemId,
        string languageCode)
    {
        if (information.InformationSource.NormalizeInformationSource() != informationSource.NormalizeInformationSource() ||
            information.SourceItemId.NormalizeSourceItemId() != sourceItemId.NormalizeSourceItemId() ||
            information.LanguageCode.NormalizeLanguageCode() != languageCode.NormalizeLanguageCode())
        {
            throw new InvalidOperationException("Animal information provider returned a different identity.");
        }

        if (string.IsNullOrWhiteSpace(information.Title))
        {
            throw new InvalidOperationException("Import animal title is empty.");
        }
    }
}

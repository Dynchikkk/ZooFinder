using ZooFinder.Application.Common.Animals.Information.Dependencies.AnimalInformation;
using ZooFinder.Application.Common.Animals.Information.Exceptions;
using ZooFinder.Application.Common.Animals.Information.Extensions;
using ZooFinder.Application.Common.Animals.Information.Validators;
using ZooFinder.Application.Common.Animals.Registration.Services.AnimalRegistration;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Language.Extensions;
using ZooFinder.Application.Common.Language.Validators;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Parks.Animals.DataSources;
using ZooFinder.Application.Features.Parks.Animals.Validators;
using ZooFinder.Application.Features.Parks.Catalog.DataSources;
using ZooFinder.Application.Features.Parks.Catalog.Validators;
using ZooFinder.Application.Features.Parks.Import.Validators;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Import.Services.ParkAnimalImport;

public sealed class ParkAnimalImportService : IParkAnimalImportService
{
    private readonly IParkCatalogDataSource _parks;
    private readonly IParkAnimalDataSource _parkAnimals;
    private readonly IAnimalInformationProvider _information;
    private readonly IAnimalRegistrationService _registration;

    public ParkAnimalImportService(IParkCatalogDataSource parks, IParkAnimalDataSource parkAnimals,
        IAnimalInformationProvider information, IAnimalRegistrationService registration)
    {
        ArgumentNullException.ThrowIfNull(parks);
        ArgumentNullException.ThrowIfNull(parkAnimals);
        ArgumentNullException.ThrowIfNull(information);
        ArgumentNullException.ThrowIfNull(registration);
        _parks = parks; _parkAnimals = parkAnimals; _information = information; _registration = registration;
    }

    public async Task<ParkAnimalImportPreviewResponse> PreviewAsync(
        PreviewParkAnimalImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkImportValidator.ValidateRows(request.Rows);
        await ValidateParkAsync(request.ParkId, cancellationToken);
        var results = new List<ParkAnimalImportRowResponse>(request.Rows.Count);
        var identities = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < request.Rows.Count; index++)
        {
            var row = request.Rows[index];
            try
            {
                string language = row.LanguageCode.NormalizeLanguageCode();
                string? description = ParkAnimalValidator.NormalizeDescription(row.LocalDescription);
                var details = await ResolveAsync(row, language, cancellationToken);
                string identity = GetIdentity(details.InformationSource, details.SourceItemId, details.LanguageCode);
                if (!identities.Add(identity)) throw new RequestValidationException("Duplicate animal in import.");
                results.Add(new ParkAnimalImportRowResponse(index,
                    new ParkAnimalImportMatchResponse(details.InformationSource, details.SourceItemId,
                        details.LanguageCode, details.Title, details.ScientificName, description), null));
            }
            catch (Exception exception) when (
                exception is RequestValidationException or NotFoundException or AnimalInformationUnavailableException)
            {
                results.Add(new ParkAnimalImportRowResponse(index, null, exception.Message));
            }
        }
        return new ParkAnimalImportPreviewResponse(results);
    }

    public async Task<ParkAnimalImportResponse> ApplyAsync(
        ApplyParkAnimalImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkImportValidator.ValidateRows(request.Rows);
        await ValidateParkAsync(request.ParkId, cancellationToken);
        var prepared = new List<(AnimalInformationDetailsResult Information, string? Description, bool Published)>();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in request.Rows)
        {
            string source = row.InformationSource.NormalizeInformationSource();
            string item = row.SourceItemId.NormalizeSourceItemId();
            string language = row.LanguageCode.NormalizeLanguageCode();
            AnimalInformationValidator.ValidateIdentity(source, item, language);
            string? description = ParkAnimalValidator.NormalizeDescription(row.LocalDescription);
            if (!identities.Add(GetIdentity(source, item, language)))
                throw new RequestValidationException("Duplicate animal in import.");
            var details = await _information.GetDetailsAsync(source, item, language, cancellationToken)
                ?? throw new NotFoundException("An import animal was not found.");
            if (GetIdentity(details.InformationSource, details.SourceItemId, details.LanguageCode) !=
                GetIdentity(source, item, language))
                throw new InvalidOperationException("Animal information provider returned a different identity.");
            if (string.IsNullOrWhiteSpace(details.Title)) throw new InvalidOperationException("Import animal title is empty.");
            prepared.Add((details, description, row.IsPublished));
        }

        // Validate the entire selection before writing. Only park-link changes form the batch transaction.
        var links = new List<ParkAnimal>();
        var registeredIds = new HashSet<Guid>();
        foreach (var row in prepared)
        {
            var registration = await _registration.RegisterAsync(row.Information, cancellationToken);
            if (!registeredIds.Add(registration.AnimalId))
                throw new RequestValidationException("Import rows resolve to the same species.");
            links.Add(new ParkAnimal { Id = Guid.NewGuid(), ParkId = request.ParkId, AnimalId = registration.AnimalId,
                LocalDescription = row.Description, IsPublished = row.Published });
        }
        var persisted = await _parkAnimals.UpsertBatchAsync(request.ParkId, links, cancellationToken);
        return new ParkAnimalImportResponse(persisted.Select(link => link.Id).ToArray());
    }

    private async Task<AnimalInformationDetailsResult> ResolveAsync(
        ParkAnimalImportRow row, string language, CancellationToken cancellationToken)
    {
        string source = row.InformationSource.NormalizeInformationSource();
        string item = row.SourceItemId.NormalizeSourceItemId();
        string common = ParkImportValidator.NormalizeName(row.CommonName);
        string scientific = ParkImportValidator.NormalizeName(row.ScientificName);
        if (source.Length > 0 || item.Length > 0)
        {
            AnimalInformationValidator.ValidateIdentity(source, item, language);
        }
        else
        {
            LanguageCodeValidator.Validate(language);
            string name = scientific.Length > 0 ? scientific : common;
            if (name.Length == 0) throw new RequestValidationException("Import row needs an animal name or source identity.");
            var matches = await _information.SearchAsync(name, language, new CursorPageRequest(null, 20), cancellationToken);
            var exact = matches.Items.Where(candidate => scientific.Length > 0
                ? string.Equals(candidate.ScientificName?.Trim(), scientific, StringComparison.OrdinalIgnoreCase)
                : string.Equals(candidate.Title.Trim(), common, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (exact.Length != 1 || matches.NextCursor != null)
                throw new RequestValidationException("Animal match is missing or ambiguous; select a source item explicitly.");
            source = exact[0].InformationSource; item = exact[0].SourceItemId;
        }
        var details = await _information.GetDetailsAsync(source, item, language, cancellationToken)
            ?? throw new NotFoundException("Import animal was not found.");
        if (GetIdentity(details.InformationSource, details.SourceItemId, details.LanguageCode) !=
            GetIdentity(source, item, language))
            throw new InvalidOperationException("Animal information provider returned a different identity.");
        if (string.IsNullOrWhiteSpace(details.Title)) throw new InvalidOperationException("Import animal title is empty.");
        return details;
    }

    private async Task ValidateParkAsync(Guid id, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(id);
        _ = await _parks.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Park was not found.");
    }

    private static string GetIdentity(string source, string item, string language) =>
        $"{source.NormalizeInformationSource()}\n{language.NormalizeLanguageCode()}\n{item.NormalizeSourceItemId()}";
}

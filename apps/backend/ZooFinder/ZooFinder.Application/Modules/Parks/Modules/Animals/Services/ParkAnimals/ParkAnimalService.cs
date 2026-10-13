using ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;
using ZooFinder.Application.Common.Animals.Exceptions;
using ZooFinder.Application.Common.Animals.Services.AnimalRegistration;
using ZooFinder.Application.Modules.Parks.DataSources;
using ZooFinder.Application.Modules.Parks.Extensions;
using ZooFinder.Application.Modules.Parks.Validators;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Animals;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Modules.Parks.Modules.Animals.Services.ParkAnimals;

public sealed class ParkAnimalService : IParkAnimalService
{
    private readonly IParkCatalogDataSource _parkCatalogDataSource;
    private readonly IParkAnimalDataSource _parkAnimalDataSource;
    private readonly IAnimalRegistrationService _animalRegistrationService;
    private readonly IAnimalInformationProvider _animalInformationProvider;

    public ParkAnimalService(
        IParkCatalogDataSource parkCatalogDataSource,
        IParkAnimalDataSource parkAnimalDataSource,
        IAnimalRegistrationService animalRegistrationService,
        IAnimalInformationProvider animalInformationProvider)
    {
        ArgumentNullException.ThrowIfNull(parkCatalogDataSource);
        ArgumentNullException.ThrowIfNull(parkAnimalDataSource);
        ArgumentNullException.ThrowIfNull(animalRegistrationService);
        ArgumentNullException.ThrowIfNull(animalInformationProvider);

        _parkCatalogDataSource = parkCatalogDataSource;
        _parkAnimalDataSource = parkAnimalDataSource;
        _animalRegistrationService = animalRegistrationService;
        _animalInformationProvider = animalInformationProvider;
    }

    public async Task<ParkAnimalResponse> GetAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        ParkAnimalValidator.ValidateId(parkAnimalId);

        ParkAnimal parkAnimal = await GetParkAnimalAsync(parkAnimalId, cancellationToken);
        AnimalInformationDetailsResult? information = await GetAnimalInformationAsync(parkAnimal.Animal, cancellationToken);

        return ToResponse(parkAnimal, information);
    }

    public async Task<OffsetPageResponse<ParkAnimalResponse>> SearchAsync(
        ParkAnimalSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateParkAsync(request.ParkId, cancellationToken);
        ParkValidator.ValidatePage(request.Page, request.PageSize);

        var pageRequest = new OffsetPageRequest(request.Page, request.PageSize);
        var page = await _parkAnimalDataSource.SearchAsync(
            request.ParkId, request.IncludeUnpublished, pageRequest, cancellationToken);
        var parkAnimals = page.Items
            .Select(parkAnimal => ToResponse(parkAnimal))
            .ToArray();

        return new OffsetPageResponse<ParkAnimalResponse>(parkAnimals, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<ParkAnimalResponse> AddAsync(AddParkAnimalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? localDescription = request.LocalDescription.NormalizeOptionalText();
        ParkAnimalValidator.ValidateDescription(localDescription);
        await ValidateParkAsync(request.ParkId, cancellationToken);

        var registrationRequest = new RegisterAnimalRequest(
            request.InformationSource, request.SourceItemId, request.LanguageCode);
        var registration = await _animalRegistrationService.RegisterAsync(registrationRequest, cancellationToken);
        var parkAnimal = new ParkAnimal
        {
            Id = Guid.NewGuid(),
            ParkId = request.ParkId,
            AnimalId = registration.AnimalId,
            LocalDescription = localDescription,
            IsPublished = request.IsPublished
        };

        ParkAnimal savedParkAnimal = await _parkAnimalDataSource.AddOrRestoreAsync(parkAnimal, cancellationToken);
        return ToResponse(savedParkAnimal);
    }

    public async Task<ParkAnimalResponse> UpdateAsync(UpdateParkAnimalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkAnimalValidator.ValidateId(request.ParkAnimalId);

        string? localDescription = request.LocalDescription.NormalizeOptionalText();
        ParkAnimalValidator.ValidateDescription(localDescription);
        ParkAnimal parkAnimal = await GetParkAnimalAsync(request.ParkAnimalId, cancellationToken);
        parkAnimal.LocalDescription = localDescription;
        parkAnimal.IsPublished = request.IsPublished;

        await _parkAnimalDataSource.UpdateAsync(parkAnimal, cancellationToken);
        return ToResponse(parkAnimal);
    }

    public Task RemoveAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        ParkAnimalValidator.ValidateId(parkAnimalId);

        return _parkAnimalDataSource.RemoveAsync(parkAnimalId, cancellationToken);
    }

    private async Task<ParkAnimal> GetParkAnimalAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        return await _parkAnimalDataSource.GetAsync(parkAnimalId, cancellationToken)
            ?? throw new NotFoundException("Park animal was not found.");
    }

    private async Task<AnimalInformationDetailsResult?> GetAnimalInformationAsync(
        Animal animal,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _animalInformationProvider.GetDetailsAsync(
                animal.InformationSource, animal.SourceItemId, animal.LanguageCode, cancellationToken);
        }
        catch (AnimalInformationUnavailableException)
        {
            return null;
        }
    }

    private async Task ValidateParkAsync(Guid parkId, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(parkId);

        if (await _parkCatalogDataSource.GetAsync(parkId, cancellationToken) == null)
        {
            throw new NotFoundException("Park was not found.");
        }
    }

    private static ParkAnimalResponse ToResponse(ParkAnimal parkAnimal, AnimalInformationDetailsResult? information = null)
    {
        Animal animal = parkAnimal.Animal;

        return new ParkAnimalResponse(
            Id: parkAnimal.Id,
            ParkId: parkAnimal.ParkId,
            AnimalId: parkAnimal.AnimalId,
            Title: information?.Title ?? animal.Title,
            ScientificName: information?.ScientificName ?? animal.ScientificName,
            ShortDescription: information?.ShortDescription ?? animal.ShortDescription,
            ImageUrl: information?.ImageUrl ?? animal.ImageUrl,
            SourceUrl: information?.SourceUrl,
            LocalDescription: parkAnimal.LocalDescription,
            IsPublished: parkAnimal.IsPublished,
            InformationSource: animal.InformationSource,
            SourceItemId: animal.SourceItemId,
            LanguageCode: animal.LanguageCode);
    }
}

using ZooFinder.Application.Common.Animals.Information.Dependencies.AnimalInformation;
using ZooFinder.Application.Common.Animals.Information.Exceptions;
using ZooFinder.Application.Common.Animals.Registration.Services.AnimalRegistration;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Parks.Animals.DataSources;
using ZooFinder.Application.Features.Parks.Animals.Validators;
using ZooFinder.Application.Features.Parks.Catalog.DataSources;
using ZooFinder.Application.Features.Parks.Catalog.Validators;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Animals.Services.ParkAnimals;

public sealed class ParkAnimalService : IParkAnimalService
{
    private readonly IParkCatalogDataSource _parks;
    private readonly IParkAnimalDataSource _parkAnimals;
    private readonly IAnimalRegistrationService _registration;
    private readonly IAnimalInformationProvider _information;

    public ParkAnimalService(
        IParkCatalogDataSource parks,
        IParkAnimalDataSource parkAnimals,
        IAnimalRegistrationService registration,
        IAnimalInformationProvider information)
    {
        ArgumentNullException.ThrowIfNull(parks);
        ArgumentNullException.ThrowIfNull(parkAnimals);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(information);
        _parks = parks;
        _parkAnimals = parkAnimals;
        _registration = registration;
        _information = information;
    }

    public async Task<ParkAnimalResponse> GetAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        ParkAnimalValidator.ValidateId(parkAnimalId);
        var link = await _parkAnimals.GetAsync(parkAnimalId, cancellationToken)
            ?? throw new NotFoundException("Park animal was not found.");
        var animal = link.Animal;
        AnimalInformationDetailsResult? details = null;
        try
        {
            details = await _information.GetDetailsAsync(animal.InformationSource, animal.SourceItemId,
                animal.LanguageCode, cancellationToken);
        }
        catch (AnimalInformationUnavailableException)
        {
            // The locally cached card and park description remain available.
        }

        return new ParkAnimalResponse(link.Id, link.ParkId, animal.Id,
            details?.Title ?? animal.Title, details?.ScientificName ?? animal.ScientificName,
            details?.ShortDescription ?? animal.ShortDescription, details?.ImageUrl ?? animal.ImageUrl,
            details?.SourceUrl, link.LocalDescription, link.IsPublished,
            animal.InformationSource, animal.SourceItemId, animal.LanguageCode);
    }

    public async Task<OffsetPageResponse<ParkAnimalResponse>> SearchAsync(
        ParkAnimalSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateParkAsync(request.ParkId, cancellationToken);
        ParkValidator.ValidatePage(request.Page, request.PageSize);
        var page = await _parkAnimals.SearchAsync(request.ParkId, request.IncludeUnpublished,
            new OffsetPageRequest(request.Page, request.PageSize), cancellationToken);
        return new OffsetPageResponse<ParkAnimalResponse>(page.Items
            .Select(link =>
                new ParkAnimalResponse(link.Id, link.ParkId, link.AnimalId, link.Animal.Title,
                    link.Animal.ScientificName, link.Animal.ShortDescription, link.Animal.ImageUrl,
                    null, link.LocalDescription, link.IsPublished, link.Animal.InformationSource,
                    link.Animal.SourceItemId, link.Animal.LanguageCode))
            .ToArray(), page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<ParkAnimalResponse> AddAsync(AddParkAnimalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string? description = ParkAnimalValidator.NormalizeDescription(request.LocalDescription);
        await ValidateParkAsync(request.ParkId, cancellationToken);
        var registration = await _registration.RegisterAsync(new RegisterAnimalRequest(
            request.InformationSource, request.SourceItemId, request.LanguageCode), cancellationToken);
        var link = await _parkAnimals.UpsertAsync(new ParkAnimal
        {
            Id = Guid.NewGuid(),
            ParkId = request.ParkId,
            AnimalId = registration.AnimalId,
            LocalDescription = description,
            IsPublished = request.IsPublished
        }, cancellationToken);

        return new ParkAnimalResponse(link.Id, link.ParkId, link.AnimalId, link.Animal.Title,
            link.Animal.ScientificName, link.Animal.ShortDescription, link.Animal.ImageUrl,
            null, link.LocalDescription, link.IsPublished, link.Animal.InformationSource,
            link.Animal.SourceItemId, link.Animal.LanguageCode);
    }

    public async Task<ParkAnimalResponse> UpdateAsync(UpdateParkAnimalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkAnimalValidator.ValidateId(request.ParkAnimalId);
        string? description = ParkAnimalValidator.NormalizeDescription(request.LocalDescription);
        var link = await _parkAnimals.GetAsync(request.ParkAnimalId, cancellationToken)
            ?? throw new NotFoundException("Park animal was not found.");
        link.LocalDescription = description;
        link.IsPublished = request.IsPublished;
        await _parkAnimals.UpdateAsync(link, cancellationToken);
        return new ParkAnimalResponse(link.Id, link.ParkId, link.AnimalId, link.Animal.Title,
            link.Animal.ScientificName, link.Animal.ShortDescription, link.Animal.ImageUrl,
            null, link.LocalDescription, link.IsPublished, link.Animal.InformationSource,
            link.Animal.SourceItemId, link.Animal.LanguageCode);
    }

    public Task RemoveAsync(Guid parkAnimalId, CancellationToken cancellationToken)
    {
        ParkAnimalValidator.ValidateId(parkAnimalId);
        return _parkAnimals.RemoveAsync(parkAnimalId, cancellationToken);
    }

    private async Task ValidateParkAsync(Guid parkId, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(parkId);
        _ = await _parks.GetAsync(parkId, cancellationToken) ?? throw new NotFoundException("Park was not found.");
    }
}

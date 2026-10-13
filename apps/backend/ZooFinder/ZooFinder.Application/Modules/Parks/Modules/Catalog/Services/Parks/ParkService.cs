using ZooFinder.Application.Modules.Parks.DataSources;
using ZooFinder.Application.Modules.Parks.Extensions;
using ZooFinder.Application.Modules.Parks.Validators;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Pagination.Contracts;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Modules.Parks.Modules.Catalog.Services.Parks;

public sealed class ParkService : IParkService
{
    private const int MaximumSearchTermLength = 200;

    private readonly IParkCatalogDataSource _parkCatalogDataSource;

    public ParkService(IParkCatalogDataSource parkCatalogDataSource)
    {
        ArgumentNullException.ThrowIfNull(parkCatalogDataSource);

        _parkCatalogDataSource = parkCatalogDataSource;
    }

    public async Task<ParkResponse> GetAsync(Guid parkId, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(parkId);

        Park park = await GetParkAsync(parkId, cancellationToken);
        return ToResponse(park);
    }

    public async Task<OffsetPageResponse<ParkResponse>> SearchAsync(
        ParkSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkValidator.ValidatePage(request.Page, request.PageSize);

        string searchTerm = request.SearchTerm.NormalizeText();

        if (searchTerm.Length > MaximumSearchTermLength)
        {
            throw new RequestValidationException("Park search term is too long.");
        }

        var pageRequest = new OffsetPageRequest(request.Page, request.PageSize);
        var page = await _parkCatalogDataSource.SearchAsync(searchTerm, request.IncludeSuspended, pageRequest, cancellationToken);
        var parks = page.Items
            .Select(ToResponse)
            .ToArray();

        return new OffsetPageResponse<ParkResponse>(parks, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<ParkResponse> CreateAsync(CreateParkRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = request.Name.NormalizeText();
        string slug = request.Slug.NormalizeText().ToLowerInvariant();
        string? description = request.Description.NormalizeOptionalText();
        string? address = request.Address.NormalizeOptionalText();

        ParkValidator.Validate(name, slug, description, address);

        var park = new Park
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = description,
            Address = address
        };

        bool wasAdded = await _parkCatalogDataSource.TryAddAsync(park, cancellationToken);

        if (!wasAdded)
        {
            throw new ConflictException("Park slug already exists.");
        }

        return ToResponse(park);
    }

    public async Task<ParkResponse> UpdateAsync(UpdateParkRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkValidator.ValidateId(request.ParkId);

        string name = request.Name.NormalizeText();
        string slug = request.Slug.NormalizeText().ToLowerInvariant();
        string? description = request.Description.NormalizeOptionalText();
        string? address = request.Address.NormalizeOptionalText();

        ParkValidator.Validate(name, slug, description, address);

        Park park = await GetParkAsync(request.ParkId, cancellationToken);
        park.Name = name;
        park.Slug = slug;
        park.Description = description;
        park.Address = address;

        bool wasUpdated = await _parkCatalogDataSource.TryUpdateAsync(park, cancellationToken);

        if (!wasUpdated)
        {
            throw new ConflictException("Park changed or slug already exists.");
        }

        return ToResponse(park);
    }

    public async Task<ParkResponse> SetStatusAsync(Guid parkId, ParkStatus status, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(parkId);
        ParkValidator.ValidateStatus(status);

        Park park = await GetParkAsync(parkId, cancellationToken);
        park.Status = status;

        bool wasUpdated = await _parkCatalogDataSource.TryUpdateAsync(park, cancellationToken);

        if (!wasUpdated)
        {
            throw new ConflictException("Park changed.");
        }

        return ToResponse(park);
    }

    private async Task<Park> GetParkAsync(Guid parkId, CancellationToken cancellationToken)
    {
        return await _parkCatalogDataSource.GetAsync(parkId, cancellationToken)
            ?? throw new NotFoundException("Park was not found.");
    }

    private static ParkResponse ToResponse(Park park)
    {
        return new ParkResponse(park.Id, park.Name, park.Slug, park.Description, park.Address, park.Status);
    }
}

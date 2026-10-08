using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Pagination.Contracts;
using ZooFinder.Application.Features.Parks.Catalog.DataSources;
using ZooFinder.Application.Features.Parks.Catalog.Validators;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Catalog.Services.Parks;

public sealed class ParkService : IParkService
{
    private readonly IParkCatalogDataSource _dataSource;

    public ParkService(IParkCatalogDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        _dataSource = dataSource;
    }

    public async Task<ParkResponse> GetAsync(Guid parkId, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(parkId);
        var park = await GetParkAsync(parkId, cancellationToken);
        return new ParkResponse(park.Id, park.Name, park.Slug, park.Description, park.Address, park.Status);
    }

    public async Task<OffsetPageResponse<ParkResponse>> SearchAsync(
        ParkSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkValidator.ValidatePage(request.Page, request.PageSize);
        string term = request.SearchTerm?.Trim() ?? string.Empty;
        if (term.Length > 200)
        {
            throw new RequestValidationException("Park search term is too long.");
        }

        var page = await _dataSource.SearchAsync(term, request.IncludeSuspended,
            new OffsetPageRequest(request.Page, request.PageSize), cancellationToken);
        return new OffsetPageResponse<ParkResponse>(page.Items
            .Select(park => new ParkResponse(park.Id, park.Name, park.Slug, park.Description, park.Address, park.Status))
            .ToArray(),
            page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<ParkResponse> CreateAsync(CreateParkRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string name = request.Name?.Trim() ?? string.Empty;
        string slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        string? description = Normalize(request.Description);
        string? address = Normalize(request.Address);
        ParkValidator.Validate(name, slug, description, address);
        var park = new Park { Id = Guid.NewGuid(), Name = name, Slug = slug, Description = description, Address = address };
        if (!await _dataSource.TryAddAsync(park, cancellationToken))
        {
            throw new ConflictException("Park slug already exists.");
        }

        return new ParkResponse(park.Id, park.Name, park.Slug, park.Description, park.Address, park.Status);
    }

    public async Task<ParkResponse> UpdateAsync(UpdateParkRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ParkValidator.ValidateId(request.ParkId);
        string name = request.Name?.Trim() ?? string.Empty;
        string slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        string? description = Normalize(request.Description);
        string? address = Normalize(request.Address);
        ParkValidator.Validate(name, slug, description, address);
        var park = await GetParkAsync(request.ParkId, cancellationToken);
        park.Name = name;
        park.Slug = slug;
        park.Description = description;
        park.Address = address;
        if (!await _dataSource.TryUpdateAsync(park, cancellationToken))
        {
            throw new ConflictException("Park changed or slug already exists.");
        }

        return new ParkResponse(park.Id, park.Name, park.Slug, park.Description, park.Address, park.Status);
    }

    public async Task<ParkResponse> SetStatusAsync(Guid parkId, ParkStatus status, CancellationToken cancellationToken)
    {
        ParkValidator.ValidateId(parkId);
        ParkValidator.ValidateStatus(status);
        var park = await GetParkAsync(parkId, cancellationToken);
        park.Status = status;
        if (!await _dataSource.TryUpdateAsync(park, cancellationToken))
        {
            throw new ConflictException("Park changed.");
        }

        return new ParkResponse(park.Id, park.Name, park.Slug, park.Description, park.Address, park.Status);
    }

    private async Task<Park> GetParkAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dataSource.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Park was not found.");
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

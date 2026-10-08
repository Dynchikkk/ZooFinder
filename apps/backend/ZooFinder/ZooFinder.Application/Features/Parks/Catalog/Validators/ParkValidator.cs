using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Pagination.Constants;
using ZooFinder.Application.Features.Parks.Catalog.Constants;
using ZooFinder.Domain.Parks;

namespace ZooFinder.Application.Features.Parks.Catalog.Validators;

public static class ParkValidator
{
    public static void ValidateId(Guid id)
    {
        if (id == Guid.Empty) throw new RequestValidationException("Park ID must not be empty.");
    }

    public static void Validate(string name, string slug, string? description, string? address)
    {
        if (name.Length == 0 || name.Length > ParkConstraints.MaximumNameLength)
            throw new RequestValidationException("Park name length is invalid.");
        if (slug.Length == 0 || slug.Length > ParkConstraints.MaximumSlugLength ||
            slug[0] == '-' || slug[^1] == '-' ||
            slug.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
            throw new RequestValidationException("Park slug must contain lowercase Latin letters, digits or hyphens.");
        if (description?.Length > ParkConstraints.MaximumDescriptionLength ||
            address?.Length > ParkConstraints.MaximumAddressLength)
            throw new RequestValidationException("Park description or address is too long.");
    }

    public static void ValidateStatus(ParkStatus status)
    {
        if (status is not (ParkStatus.Active or ParkStatus.Suspended))
            throw new RequestValidationException("Park status is invalid.");
    }

    public static void ValidatePage(int pageIndex, int pageSize)
    {
        if (pageIndex < 0 || pageSize < 1 || pageSize > PaginationDefaults.MaximumPageSize ||
            (long)pageIndex * pageSize > int.MaxValue)
            throw new RequestValidationException("Park pagination is invalid.");
    }
}

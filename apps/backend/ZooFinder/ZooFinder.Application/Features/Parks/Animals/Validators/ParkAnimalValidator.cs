using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Features.Parks.Animals.Constants;

namespace ZooFinder.Application.Features.Parks.Animals.Validators;

public static class ParkAnimalValidator
{
    public static void ValidateId(Guid id)
    {
        if (id == Guid.Empty) throw new RequestValidationException("Park animal ID must not be empty.");
    }

    public static string? NormalizeDescription(string? value)
    {
        string? description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (description?.Length > ParkAnimalConstraints.MaximumLocalDescriptionLength)
            throw new RequestValidationException("Local animal description is too long.");
        return description;
    }
}

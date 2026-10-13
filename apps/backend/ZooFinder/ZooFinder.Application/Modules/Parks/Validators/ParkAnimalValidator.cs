using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Modules.Parks.Constants;

namespace ZooFinder.Application.Modules.Parks.Validators;

public static class ParkAnimalValidator
{
    public static void ValidateId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException("Park animal ID must not be empty.");
        }
    }

    public static void ValidateDescription(string? description)
    {
        if (description?.Length > ParkAnimalConstraints.MaximumLocalDescriptionLength)
        {
            throw new RequestValidationException("Local animal description is too long.");
        }
    }
}

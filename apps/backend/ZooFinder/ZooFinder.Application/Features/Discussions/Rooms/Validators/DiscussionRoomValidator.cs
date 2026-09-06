using ZooFinder.Application.Common.ErrorHandling.Exceptions;

namespace ZooFinder.Application.Features.Discussions.Rooms.Validators;

public static class DiscussionRoomValidator
{
    public static void ValidateAnimalId(Guid animalId)
    {
        if (animalId == Guid.Empty)
        {
            throw new RequestValidationException("Animal ID must not be empty.");
        }
    }
}

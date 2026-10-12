using ZooFinder.Application.Shared.ErrorHandling.Exceptions;

namespace ZooFinder.Application.Modules.Discussions.Modules.Rooms.Validators;

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

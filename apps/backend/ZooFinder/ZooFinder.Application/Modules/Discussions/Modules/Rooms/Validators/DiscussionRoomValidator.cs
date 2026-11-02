using ZooFinder.Application.Shared.ErrorHandling.Exceptions;

namespace ZooFinder.Application.Modules.Discussions.Modules.Rooms.Validators;

public static class DiscussionRoomValidator
{
    public static void ValidateRoomId(Guid roomId)
    {
        if (roomId == Guid.Empty)
        {
            throw new RequestValidationException("Discussion room ID must not be empty.");
        }
    }

    public static void ValidateAnimalId(Guid animalId)
    {
        if (animalId == Guid.Empty)
        {
            throw new RequestValidationException("Animal ID must not be empty.");
        }
    }
}

using ZooFinder.Domain.Animals;
using ZooFinder.Domain.Discussions;

namespace ZooFinder.Application.Common.Animals.Registration.DataSources;

public interface IAnimalRegistrationDataSource
{
    Task<DiscussionRoom?> GetRegisteredRoomAsync(
        string informationSource,
        string sourceItemId,
        string languageCode,
        CancellationToken cancellationToken);

    // Atomically reuses a matching species and room or creates both.
    Task<DiscussionRoom> GetOrCreateAsync(
        Animal animal,
        DiscussionRoom generalRoom,
        CancellationToken cancellationToken);
}

using ZooFinder.Application.Common.AnimalInformation.Extensions;
using ZooFinder.Application.Common.AnimalInformation.Interfaces;
using ZooFinder.Application.Common.AnimalInformation.Validators;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Features.Discussions.Common.Interfaces;
using ZooFinder.Application.Features.Discussions.Common.Validators;
using ZooFinder.Application.Features.Discussions.Rooms.Constants;
using ZooFinder.Application.Features.Discussions.Rooms.Contracts;
using ZooFinder.Application.Features.Discussions.Rooms.Interfaces;
using ZooFinder.Application.Features.Discussions.Rooms.Validators;
using ZooFinder.Domain.Animals;
using ZooFinder.Domain.Discussions;
using ZooFinder.Domain.Users;

namespace ZooFinder.Application.Features.Discussions.Rooms.Services;

public sealed class DiscussionRoomService : IDiscussionRoomService
{
    private readonly IDiscussionRepository _discussionRepository;
    private readonly IAnimalInformationProvider _animalInformationProvider;
    private readonly TimeProvider _timeProvider;

    public DiscussionRoomService(
        IDiscussionRepository discussionRepository,
        IAnimalInformationProvider animalInformationProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(discussionRepository);
        ArgumentNullException.ThrowIfNull(animalInformationProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _discussionRepository = discussionRepository;
        _animalInformationProvider = animalInformationProvider;
        _timeProvider = timeProvider;
    }

    public async Task<DiscussionRoomResponse?> GetGeneralRoomAsync(
        Guid animalId,
        CancellationToken cancellationToken)
    {
        DiscussionRoomValidator.ValidateAnimalId(animalId);

        var room = await _discussionRepository.GetGeneralRoomByAnimalIdAsync(
            animalId,
            cancellationToken);

        if (room == null || room.IsDeleted)
        {
            return null;
        }

        return new DiscussionRoomResponse(
            room.Id,
            room.AnimalId,
            room.Type,
            room.Name,
            room.Description,
            room.IsClosed);
    }

    public async Task<DiscussionRoomResponse> CreateDiscussionAsync(
        Guid currentUserAccountId,
        CreateDiscussionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string informationSource = request.InformationSource.NormalizeInformationSource();
        string sourceItemId = request.SourceItemId.NormalizeSourceItemId();
        string languageCode = request.LanguageCode.NormalizeLanguageCode();

        DiscussionUserValidator.ValidateUserAccountId(currentUserAccountId);
        AnimalInformationValidator.ValidateIdentity(informationSource, sourceItemId, languageCode);

        UserAccount userAccount = await _discussionRepository.GetUserAccountWithProfileByIdAsync(currentUserAccountId, cancellationToken)
            ?? throw new UnauthorizedException("User account was not found.");
        DiscussionUserValidator.ValidateUserCanWrite(userAccount);

        DiscussionRoom? existingRoom = await _discussionRepository
            .GetGeneralRoomByAnimalSourceAsync(
                informationSource,
                sourceItemId,
                languageCode,
                cancellationToken);

        if (existingRoom != null)
        {
            if (existingRoom.IsDeleted)
            {
                throw new InvalidOperationException(
                    "A sourced animal has a deleted General discussion room.");
            }

            return new DiscussionRoomResponse(
                existingRoom.Id,
                existingRoom.AnimalId,
                existingRoom.Type,
                existingRoom.Name,
                existingRoom.Description,
                existingRoom.IsClosed);
        }

        var animalInformation = await _animalInformationProvider.GetDetailsAsync(informationSource, sourceItemId, languageCode, cancellationToken)
            ?? throw new NotFoundException("Animal information was not found.");

        DateTime currentTime = _timeProvider.GetUtcNow().UtcDateTime;

        var animal = new Animal
        {
            Id = Guid.NewGuid(),
            InformationSource = animalInformation.InformationSource,
            SourceItemId = animalInformation.SourceItemId,
            LanguageCode = animalInformation.LanguageCode,
            Title = animalInformation.Title,
            ScientificName = animalInformation.ScientificName,
            ShortDescription = animalInformation.ShortDescription,
            ImageUrl = animalInformation.ImageUrl,
            LastSynchronizedAtUtc = currentTime,
            CreatedAtUtc = currentTime,
            UpdatedAtUtc = currentTime
        };

        var generalRoom = new DiscussionRoom
        {
            Id = Guid.NewGuid(),
            AnimalId = animal.Id,
            Animal = animal,
            Type = DiscussionRoomType.General,
            Name = DiscussionRoomDefaults.GeneralRoomName,
            CreatedAtUtc = currentTime,
            UpdatedAtUtc = currentTime
        };

        animal.DiscussionRooms.Add(generalRoom);

        await _discussionRepository.AddDiscussionAsync(
            animal,
            generalRoom,
            cancellationToken);

        return new DiscussionRoomResponse(
            generalRoom.Id,
            animal.Id,
            generalRoom.Type,
            generalRoom.Name,
            generalRoom.Description,
            generalRoom.IsClosed);
    }
}

using ZooFinder.Application.Common.Animals.Registration.Services.AnimalRegistration;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Features.Discussions.Common.DataSources;
using ZooFinder.Application.Features.Discussions.Common.Validators;
using ZooFinder.Application.Features.Discussions.Rooms.Validators;

namespace ZooFinder.Application.Features.Discussions.Rooms.Services.DiscussionRooms;

public sealed class DiscussionRoomService : IDiscussionRoomService
{
    private readonly IDiscussionDataSource _discussionDataSource;
    private readonly IAnimalRegistrationService _registration;

    public DiscussionRoomService(IDiscussionDataSource discussionDataSource, IAnimalRegistrationService registration)
    {
        ArgumentNullException.ThrowIfNull(discussionDataSource);
        ArgumentNullException.ThrowIfNull(registration);
        _discussionDataSource = discussionDataSource;
        _registration = registration;
    }

    public async Task<DiscussionRoomResponse?> GetGeneralRoomAsync(Guid animalId, CancellationToken cancellationToken)
    {
        DiscussionRoomValidator.ValidateAnimalId(animalId);
        var room = await _discussionDataSource.GetGeneralRoomByAnimalIdAsync(animalId, cancellationToken);
        if (room == null || room.IsDeleted)
        {
            return null;
        }

        return new DiscussionRoomResponse(room.Id, room.AnimalId, room.Type, room.Name, room.Description, room.IsClosed);
    }

    public async Task<DiscussionRoomResponse> CreateDiscussionAsync(
        Guid currentUserAccountId,
        CreateDiscussionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        DiscussionUserValidator.ValidateUserAccountId(currentUserAccountId);
        var user = await _discussionDataSource.GetUserAccountWithProfileByIdAsync(currentUserAccountId, cancellationToken)
            ?? throw new UnauthorizedException("User account was not found.");
        DiscussionUserValidator.ValidateUserCanWrite(user);
        var registration = await _registration.RegisterAsync(new RegisterAnimalRequest(
            request.InformationSource, request.SourceItemId, request.LanguageCode), cancellationToken);
        var room = await _discussionDataSource.GetRoomByIdAsync(registration.GeneralRoomId, cancellationToken)
            ?? throw new NotFoundException("General discussion room was not found.");
        return new DiscussionRoomResponse(room.Id, room.AnimalId, room.Type, room.Name, room.Description, room.IsClosed);
    }
}

using ZooFinder.Application.Common.Animals.Information.Dependencies.AnimalInformation;
using ZooFinder.Application.Common.Animals.Information.Extensions;
using ZooFinder.Application.Common.Animals.Information.Validators;
using ZooFinder.Application.Common.Animals.Registration.Constants;
using ZooFinder.Application.Common.Animals.Registration.DataSources;
using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Language.Extensions;
using ZooFinder.Domain.Animals;
using ZooFinder.Domain.Discussions;

namespace ZooFinder.Application.Common.Animals.Registration.Services.AnimalRegistration;

public sealed class AnimalRegistrationService : IAnimalRegistrationService
{
    private readonly IAnimalInformationProvider _informationProvider;
    private readonly IAnimalRegistrationDataSource _dataSource;
    private readonly TimeProvider _timeProvider;

    public AnimalRegistrationService(
        IAnimalInformationProvider informationProvider,
        IAnimalRegistrationDataSource dataSource,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(informationProvider);
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _informationProvider = informationProvider;
        _dataSource = dataSource;
        _timeProvider = timeProvider;
    }

    public async Task<AnimalRegistrationResult> RegisterAsync(RegisterAnimalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string source = request.InformationSource.NormalizeInformationSource();
        string itemId = request.SourceItemId.NormalizeSourceItemId();
        string language = request.LanguageCode.NormalizeLanguageCode();
        AnimalInformationValidator.ValidateIdentity(source, itemId, language);
        var existing = await _dataSource.GetRegisteredRoomAsync(source, itemId, language, cancellationToken);
        if (existing != null)
        {
            return CreateResult(existing);
        }

        var information = await _informationProvider.GetDetailsAsync(source, itemId, language, cancellationToken)
            ?? throw new NotFoundException("Animal information was not found.");
        if (information.InformationSource.NormalizeInformationSource() != source ||
            information.SourceItemId.NormalizeSourceItemId() != itemId ||
            information.LanguageCode.NormalizeLanguageCode() != language)
        {
            throw new InvalidOperationException("Animal information provider returned a different identity.");
        }

        return await RegisterAsync(information, cancellationToken);
    }

    public async Task<AnimalRegistrationResult> RegisterAsync(
        AnimalInformationDetailsResult information,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(information);
        string source = information.InformationSource.NormalizeInformationSource();
        string itemId = information.SourceItemId.NormalizeSourceItemId();
        string language = information.LanguageCode.NormalizeLanguageCode();
        AnimalInformationValidator.ValidateIdentity(source, itemId, language);
        string title = information.Title?.Trim() ?? string.Empty;
        if (title.Length == 0)
        {
            throw new InvalidOperationException("Animal information provider returned an empty title.");
        }

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        var animal = new Animal
        {
            Id = Guid.NewGuid(),
            InformationSource = source,
            SourceItemId = itemId,
            LanguageCode = language,
            Title = title,
            ScientificName = Normalize(information.ScientificName),
            ShortDescription = Normalize(information.ShortDescription),
            ImageUrl = Normalize(information.ImageUrl),
            LastSynchronizedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var room = new DiscussionRoom
        {
            Id = Guid.NewGuid(),
            AnimalId = animal.Id,
            Animal = animal,
            Type = DiscussionRoomType.General,
            Name = AnimalRegistrationDefaults.GeneralRoomName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        animal.DiscussionRooms.Add(room);
        return CreateResult(await _dataSource.GetOrCreateAsync(animal, room, cancellationToken));
    }

    private static AnimalRegistrationResult CreateResult(DiscussionRoom room)
    {
        if (room.IsDeleted || room.Type != DiscussionRoomType.General || room.AnimalId == Guid.Empty)
        {
            throw new InvalidOperationException("Animal registration returned an invalid General room.");
        }

        return new AnimalRegistrationResult(room.AnimalId, room.Id);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

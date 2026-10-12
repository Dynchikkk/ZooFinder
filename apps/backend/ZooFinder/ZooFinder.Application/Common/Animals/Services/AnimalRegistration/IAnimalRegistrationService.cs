using ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;

namespace ZooFinder.Application.Common.Animals.Services.AnimalRegistration;

public interface IAnimalRegistrationService
{
    Task<AnimalRegistrationResult> RegisterAsync(
        RegisterAnimalRequest request,
        CancellationToken cancellationToken);

    Task<AnimalRegistrationResult> RegisterAsync(
        AnimalInformationDetailsResult information,
        CancellationToken cancellationToken);
}

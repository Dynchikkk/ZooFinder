using ZooFinder.Application.Common.Animals.Information.Dependencies.AnimalInformation;

namespace ZooFinder.Application.Common.Animals.Registration.Services.AnimalRegistration;

public interface IAnimalRegistrationService
{
    Task<AnimalRegistrationResult> RegisterAsync(
        RegisterAnimalRequest request,
        CancellationToken cancellationToken);

    Task<AnimalRegistrationResult> RegisterAsync(
        AnimalInformationDetailsResult information,
        CancellationToken cancellationToken);
}

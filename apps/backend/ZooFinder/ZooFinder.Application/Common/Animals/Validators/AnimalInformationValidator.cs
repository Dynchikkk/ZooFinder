using ZooFinder.Application.Common.Animals.Constants;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Language.Validators;

namespace ZooFinder.Application.Common.Animals.Validators;

public static class AnimalInformationValidator
{
    public static void ValidateIdentity(string informationSource, string sourceItemId, string languageCode)
    {
        if (informationSource.Length == 0 ||
            informationSource.Length > AnimalInformationConstraints.MaximumInformationSourceLength)
        {
            throw new RequestValidationException(
                $"Information source length must be between 1 and " +
                $"{AnimalInformationConstraints.MaximumInformationSourceLength} characters.");
        }

        if (sourceItemId.Length == 0 || sourceItemId.Length > AnimalInformationConstraints.MaximumSourceItemIdLength)
        {
            throw new RequestValidationException(
                $"Source item ID length must be between 1 and " +
                $"{AnimalInformationConstraints.MaximumSourceItemIdLength} characters.");
        }

        LanguageCodeValidator.Validate(languageCode);
    }
}

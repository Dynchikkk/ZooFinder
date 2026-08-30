using ZooFinder.Application.Common.Constants;
using ZooFinder.Application.Common.Exceptions;

namespace ZooFinder.Application.Features.Animals.Common.Validators;

public static class AnimalInformationValidator
{
    public static void ValidateLanguageCode(string languageCode)
    {
        if (!LanguageCodes.IsSupported(languageCode))
        {
            throw new RequestValidationException(
                $"Language code must be '{LanguageCodes.English}' or '{LanguageCodes.Russian}'.");
        }
    }
}

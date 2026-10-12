using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Language.Constants;

namespace ZooFinder.Application.Shared.Language.Validators;

public static class LanguageCodeValidator
{
    public static void Validate(string languageCode)
    {
        if (!LanguageCodes.IsSupported(languageCode))
        {
            throw new RequestValidationException(
                $"Language code must be '{LanguageCodes.English}' or '{LanguageCodes.Russian}'.");
        }
    }
}

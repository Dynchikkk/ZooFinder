using ZooFinder.Application.Common.ErrorHandling.Exceptions;
using ZooFinder.Application.Common.Language.Constants;

namespace ZooFinder.Application.Common.Language.Validators;

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

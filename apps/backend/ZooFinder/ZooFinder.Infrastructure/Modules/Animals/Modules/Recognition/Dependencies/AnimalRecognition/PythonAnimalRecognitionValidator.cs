using System.Net.Http.Headers;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Language.Validators;

namespace ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;

internal static class PythonAnimalRecognitionValidator
{
    public static void ValidateSettings(PythonAnimalRecognitionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateBaseUrl(settings.BaseUrl);

        if (settings.RequestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Recognition request timeout must be positive.");
        }

        if (settings.MaximumResponseBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Recognition response size limit must be positive.");
        }

        ValidateSupportedImageTypes(settings.SupportedImageTypes);
    }

    public static void ValidateRequest(AnimalRecognitionProviderRequest request, IReadOnlyList<string> supportedImageTypes)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ImageStream == null || !request.ImageStream.CanRead)
        {
            throw new RequestValidationException("Recognition image stream must be readable.");
        }

        LanguageCodeValidator.Validate(request.LanguageCode);
        ValidateContentType(request.ContentType, supportedImageTypes);
        ValidateCandidates(request.Candidates);
    }

    public static void ValidateResponse(PythonRecognitionResponse response)
    {
        if (response.Alternatives == null || response.Alternatives.Any(candidate => candidate == null))
        {
            throw new InvalidOperationException("Recognition service returned invalid alternatives.");
        }

        if (response.Execution != null && response.Execution.InferenceMilliseconds == null)
        {
            throw new InvalidOperationException("Recognition service omitted inference duration.");
        }
    }

    private static void ValidateCandidates(IReadOnlyList<AnimalRecognitionContextCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var seenIds = new HashSet<Guid>();
        foreach (var candidate in candidates)
        {
            if (candidate == null || candidate.AnimalId == Guid.Empty)
            {
                throw new RequestValidationException("Recognition context contains an invalid animal ID.");
            }

            if (!seenIds.Add(candidate.AnimalId))
            {
                throw new RequestValidationException("Recognition context contains duplicate animal IDs.");
            }

            if (string.IsNullOrWhiteSpace(candidate.CommonName) && string.IsNullOrWhiteSpace(candidate.ScientificName))
            {
                throw new RequestValidationException("Recognition context contains an animal without a name.");
            }
        }
    }

    private static void ValidateContentType(string contentType, IReadOnlyList<string> supportedImageTypes)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out var header) || header.MediaType == null)
        {
            throw new RequestValidationException("Recognition image content type is invalid.");
        }

        if (!supportedImageTypes.Contains(header.MediaType, StringComparer.OrdinalIgnoreCase))
        {
            throw new RequestValidationException("Recognition image type is not enabled in the provider settings.");
        }
    }

    private static void ValidateSupportedImageTypes(IReadOnlyList<string> supportedImageTypes)
    {
        ArgumentNullException.ThrowIfNull(supportedImageTypes);

        if (supportedImageTypes.Count == 0)
        {
            throw new ArgumentException("At least one recognition image type must be configured.");
        }

        foreach (string contentType in supportedImageTypes)
        {
            if (!MediaTypeHeaderValue.TryParse(contentType, out var header) || header.MediaType == null)
            {
                throw new ArgumentException("Configured recognition image type is invalid.");
            }

            if (!header.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || header.Parameters.Count != 0)
            {
                throw new ArgumentException("Configured recognition image types must be image media types without parameters.");
            }
        }
    }

    private static void ValidateBaseUrl(Uri? baseUrl)
    {
        if (baseUrl == null || !baseUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("An absolute recognition service BaseUrl is required.");
        }

        if (baseUrl.Scheme != Uri.UriSchemeHttp && baseUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Recognition service BaseUrl must use HTTP or HTTPS.");
        }

        if (baseUrl.UserInfo.Length != 0 || baseUrl.Query.Length != 0 || baseUrl.Fragment.Length != 0)
        {
            throw new ArgumentException("Recognition service BaseUrl cannot contain credentials, query or fragment.");
        }

        if (!baseUrl.AbsolutePath.EndsWith('/'))
        {
            throw new ArgumentException("Recognition service BaseUrl must end with a slash.");
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Exceptions;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;

namespace ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;

internal sealed class PythonAnimalRecognitionApiClient
{
    private const string RecognitionPath = "v1/recognize";
    private const string ImagePartName = "image";
    private const string MetadataPartName = "metadata";
    private const string ImageFileName = "image";
    private const string JsonMediaType = "application/json";
    private const string InvalidImageCode = "invalid_image";
    private const string ImageTooLargeCode = "image_too_large";
    private const string UnsupportedImageTypeCode = "unsupported_image_type";
    private const int MaximumJsonDepth = 32;
    private const int ReadBufferSizeBytes = 8192;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { MaxDepth = MaximumJsonDepth };

    private readonly HttpClient _httpClient;
    private readonly PythonAnimalRecognitionSettings _settings;
    private readonly Uri _endpoint;

    public PythonAnimalRecognitionApiClient(HttpClient httpClient, PythonAnimalRecognitionSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
        _endpoint = new Uri(settings.BaseUrl!, RecognitionPath);
    }

    public async Task<PythonRecognitionResponse> RecognizeAsync(
        AnimalRecognitionProviderRequest input,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_settings.RequestTimeout);
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
        request.Content = CreateMultipartContent(input);

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            ValidateAvailability(response.StatusCode);
            byte[] body = await ReadBodyAsync(response.Content, timeout.Token);
            ValidateJsonContentType(response.Content);
            ValidateStatus(response.StatusCode, body);
            return JsonSerializer.Deserialize<PythonRecognitionResponse>(body, JsonOptions)
                ?? throw new InvalidOperationException("Recognition service returned no result.");
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AnimalRecognitionUnavailableException("Recognition request timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AnimalRecognitionUnavailableException("Recognition service could not be reached.", exception);
        }
        catch (IOException exception)
        {
            throw new AnimalRecognitionUnavailableException("Recognition response could not be read.", exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Recognition service returned invalid JSON.", exception);
        }
    }

    private static MultipartFormDataContent CreateMultipartContent(AnimalRecognitionProviderRequest input)
    {
        var candidates = input.Candidates
            .Select(candidate => new PythonRecognitionContextCandidate(
                candidate.AnimalId, candidate.CommonName, candidate.ScientificName))
            .ToArray();

        var metadata = new PythonRecognitionMetadata(input.LanguageCode, candidates);
        var content = new MultipartFormDataContent();
        content.Add(new RecognitionImageContent(input.ImageStream, input.ContentType), ImagePartName, ImageFileName);
        content.Add(JsonContent.Create(metadata, options: JsonOptions), MetadataPartName);
        return content;
    }

    private static void ValidateAvailability(HttpStatusCode status)
    {
        if (status == HttpStatusCode.RequestTimeout || status == HttpStatusCode.TooManyRequests)
        {
            throw new AnimalRecognitionUnavailableException("Recognition service is busy or unavailable.");
        }

        if (status >= HttpStatusCode.InternalServerError)
        {
            throw new AnimalRecognitionUnavailableException("Recognition service failed to process the request.");
        }
    }

    private static void ValidateJsonContentType(HttpContent content)
    {
        if (!string.Equals(content.Headers.ContentType?.MediaType, JsonMediaType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Recognition service returned a non-JSON response.");
        }
    }

    private static void ValidateStatus(HttpStatusCode status, byte[] body)
    {
        if (status == HttpStatusCode.OK)
        {
            return;
        }

        var error = JsonSerializer.Deserialize<PythonRecognitionError>(body, JsonOptions);
        if (status == HttpStatusCode.UnprocessableEntity && error?.Code == InvalidImageCode)
        {
            throw new RequestValidationException("The image could not be decoded for recognition.");
        }

        if (status == HttpStatusCode.RequestEntityTooLarge && error?.Code == ImageTooLargeCode)
        {
            throw new RequestValidationException("Recognition image exceeds the maximum size.");
        }

        if (status == HttpStatusCode.UnsupportedMediaType && error?.Code == UnsupportedImageTypeCode)
        {
            throw new RequestValidationException("Recognition image type is not supported.");
        }

        throw new InvalidOperationException("Recognition service rejected the HTTP protocol request.");
    }

    private async Task<byte[]> ReadBodyAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > _settings.MaximumResponseBytes)
        {
            throw new InvalidOperationException("Recognition response exceeds the configured size limit.");
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var body = new MemoryStream();
        byte[] buffer = new byte[ReadBufferSizeBytes];
        int count;

        while ((count = await stream.ReadAsync(buffer, cancellationToken)) != 0)
        {
            if (body.Length + count > _settings.MaximumResponseBytes)
            {
                throw new InvalidOperationException("Recognition response exceeds the configured size limit.");
            }

            body.Write(buffer, 0, count);
        }

        return body.ToArray();
    }
}

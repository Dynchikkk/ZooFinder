using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using ZooFinder.Application.Common.Animals.Exceptions;

namespace ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;

internal sealed class WikiApiClient
{
    private const int MaximumJsonDepth = 32;
    private const int ReadBufferSizeBytes = 8192;
    private const int ApiFormatVersion = 2;
    private const int MaximumReplicationLagSeconds = 5;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { MaxDepth = MaximumJsonDepth };

    private readonly HttpClient _httpClient;
    private readonly WikiAnimalInformationSettings _settings;

    public WikiApiClient(HttpClient httpClient, WikiAnimalInformationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        WikiAnimalInformationSettingsValidator.Validate(settings);

        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<WikiApiResponse> QueryAsync(
        string languageCode,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_settings.RequestTimeout);

        Uri requestUri = BuildRequestUri(languageCode, parameters);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.UserAgent.ParseAdd(_settings.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();

            byte[] body = await ReadBodyAsync(response.Content, timeout.Token);
            var result = JsonSerializer.Deserialize<WikiApiResponse>(body, JsonOptions)
                ?? throw new InvalidOperationException("Wikipedia returned an empty response.");

            if (result.Error != null)
            {
                if (result.Error.Code == "maxlag" || result.Error.Code == "ratelimited" ||
                    result.Error.Code == "readonly" || result.Error.Code == "search-error")
                {
                    throw new AnimalInformationUnavailableException("Wikipedia is temporarily unavailable.");
                }

                throw new InvalidOperationException("Wikipedia rejected the information request.");
            }

            return result;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AnimalInformationUnavailableException("The Wikipedia request timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AnimalInformationUnavailableException("Wikipedia could not be reached.", exception);
        }
        catch (IOException exception)
        {
            throw new AnimalInformationUnavailableException("The Wikipedia response could not be read.", exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Wikipedia returned invalid information.", exception);
        }
    }

    private static Uri BuildRequestUri(string languageCode, IReadOnlyDictionary<string, string> parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var parameter in parameters)
        {
            query[parameter.Key] = parameter.Value;
        }

        query["action"] = "query";
        query["format"] = "json";
        query["formatversion"] = ApiFormatVersion.ToString(CultureInfo.InvariantCulture);
        query["maxlag"] = MaximumReplicationLagSeconds.ToString(CultureInfo.InvariantCulture);

        var uriBuilder = new UriBuilder(Uri.UriSchemeHttps, $"{languageCode}.wikipedia.org")
        {
            Path = "/w/api.php",
            Query = query.ToString()
        };

        return uriBuilder.Uri;
    }

    private async Task<byte[]> ReadBodyAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > _settings.MaximumResponseBytes)
        {
            throw new InvalidOperationException("The Wikipedia response exceeds the configured limit.");
        }

        using var body = new MemoryStream();
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        byte[] buffer = new byte[ReadBufferSizeBytes];
        int count;

        while ((count = await stream.ReadAsync(buffer, cancellationToken)) != 0)
        {
            if (body.Length + count > _settings.MaximumResponseBytes)
            {
                throw new InvalidOperationException("The Wikipedia response exceeds the configured limit.");
            }

            body.Write(buffer, 0, count);
        }

        return body.ToArray();
    }
}

using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using ZooFinder.Application.Common.Animals.Dependencies.AnimalInformation;
using ZooFinder.Application.Shared.Language.Validators;
using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;

public sealed class WikiAnimalInformationProvider : IAnimalInformationProvider
{
    private const string InformationSource = "wikipedia";
    private const string SearchCacheCategory = "search";
    private const string DetailsCacheCategory = "details";
    private const string DisambiguationProperty = "disambiguation";
    private const string EnabledParameterValue = "1";
    private const int ArticleNamespace = 0;
    private const int ThumbnailSize = 640;
    private const int MaximumExtractCharacters = 1200;
    private const int ExtractLimit = 1;
    private const int CachedResponseSize = 1;

    private readonly WikiApiClient _client;
    private readonly IMemoryCache _cache;
    private readonly WikiAnimalInformationSettings _settings;

    public WikiAnimalInformationProvider(HttpClient httpClient, IMemoryCache cache, WikiAnimalInformationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(cache);

        _client = new WikiApiClient(httpClient, settings);
        _cache = cache;
        _settings = settings;
    }

    public async Task<CursorPageResponse<AnimalInformationSearchResult>> SearchAsync(
        string searchTerm,
        string languageCode,
        CursorPageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        WikiAnimalInformationValidator.ValidateSearch(searchTerm, languageCode, pageRequest);
        cancellationToken.ThrowIfCancellationRequested();

        string scope = WikiSearchCursor.GetScope(searchTerm, languageCode, pageRequest.Limit);
        WikiSearchCursor? cursor = WikiSearchCursor.Parse(pageRequest, scope);
        var cacheKey = (typeof(WikiAnimalInformationProvider), SearchCacheCategory, scope, pageRequest.Cursor);
        if (_cache.TryGetValue(cacheKey, out CursorPageResponse<AnimalInformationSearchResult>? cached) && cached != null)
        {
            return cached;
        }

        var parameters = CreateSearchParameters(searchTerm, pageRequest.Limit, cursor);
        WikiApiResponse response = await _client.QueryAsync(languageCode, parameters, cancellationToken);
        var result = CreateSearchResult(response, languageCode, pageRequest.Limit, scope, cursor);
        _cache.Set(cacheKey, result, CreateCacheOptions(_settings.SearchCacheLifetime));
        return result;
    }

    public async Task<AnimalInformationDetailsResult?> GetDetailsAsync(
        string informationSource,
        string sourceItemId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        LanguageCodeValidator.Validate(languageCode);
        cancellationToken.ThrowIfCancellationRequested();

        if (informationSource != InformationSource)
        {
            return null;
        }

        long pageId = WikiAnimalInformationValidator.ValidateAndGetPageId(sourceItemId);
        var cacheKey = (typeof(WikiAnimalInformationProvider), DetailsCacheCategory, languageCode, pageId);
        if (_cache.TryGetValue(cacheKey, out AnimalInformationDetailsResult? cached) && cached != null)
        {
            return cached;
        }

        var parameters = CreateDetailsParameters(sourceItemId);
        WikiApiResponse response = await _client.QueryAsync(languageCode, parameters, cancellationToken);
        WikiPage page = WikiAnimalInformationValidator.ValidateAndGetDetailsPage(response, pageId);
        if (!IsAvailable(page))
        {
            return null;
        }

        AnimalInformationDetailsResult result = CreateDetailsResult(page, sourceItemId, languageCode);
        _cache.Set(cacheKey, result, CreateCacheOptions(_settings.DetailsCacheLifetime));
        return result;
    }

    private static Dictionary<string, string> CreateSearchParameters(string searchTerm, int limit, WikiSearchCursor? cursor)
    {
        var parameters = CreatePageParameters("info|pageimages|pageprops");
        parameters["generator"] = "search";
        parameters["gsrnamespace"] = ArticleNamespace.ToString(CultureInfo.InvariantCulture);
        parameters["gsrsort"] = "relevance";
        parameters["gsrsearch"] = searchTerm;
        parameters["gsrlimit"] = limit.ToString(CultureInfo.InvariantCulture);

        if (cursor != null)
        {
            parameters["gsroffset"] = cursor.Offset.ToString(CultureInfo.InvariantCulture);
            parameters["continue"] = cursor.Token;
        }

        return parameters;
    }

    private static Dictionary<string, string> CreateDetailsParameters(string sourceItemId)
    {
        var parameters = CreatePageParameters("info|pageimages|pageprops|extracts");
        parameters["pageids"] = sourceItemId;
        parameters["exintro"] = EnabledParameterValue;
        parameters["explaintext"] = EnabledParameterValue;
        parameters["exchars"] = MaximumExtractCharacters.ToString(CultureInfo.InvariantCulture);
        parameters["exlimit"] = ExtractLimit.ToString(CultureInfo.InvariantCulture);
        return parameters;
    }

    private static Dictionary<string, string> CreatePageParameters(string properties)
    {
        return new Dictionary<string, string>
        {
            ["prop"] = properties,
            ["piprop"] = "thumbnail",
            ["pithumbsize"] = ThumbnailSize.ToString(CultureInfo.InvariantCulture),
            ["ppprop"] = DisambiguationProperty
        };
    }

    private static CursorPageResponse<AnimalInformationSearchResult> CreateSearchResult(
        WikiApiResponse response,
        string languageCode,
        int limit,
        string scope,
        WikiSearchCursor? cursor)
    {
        IReadOnlyList<WikiPage> pages = WikiAnimalInformationValidator.ValidateAndGetSearchPages(response, limit);
        var items = new List<AnimalInformationSearchResult>(pages.Count);
        foreach (WikiPage page in pages.OrderBy(page => page.Index))
        {
            if (!IsAvailable(page))
            {
                continue;
            }

            items.Add(new AnimalInformationSearchResult(
                InformationSource, page.PageId.ToString(CultureInfo.InvariantCulture), languageCode,
                page.Title!, null, GetImageUrl(page)));
        }

        string? nextCursor = response.Continuation == null
            ? null
            : WikiSearchCursor.Encode(scope, response.Continuation, cursor?.Offset ?? 0);
        return new CursorPageResponse<AnimalInformationSearchResult>(items.AsReadOnly(), nextCursor);
    }

    private static AnimalInformationDetailsResult CreateDetailsResult(WikiPage page, string sourceItemId, string languageCode)
    {
        string? description = string.IsNullOrWhiteSpace(page.Extract) ? null : page.Extract.Trim();
        return new AnimalInformationDetailsResult(
            InformationSource, sourceItemId, languageCode, page.Title!, null, description, GetImageUrl(page),
            $"https://{languageCode}.wikipedia.org/?curid={sourceItemId}");
    }

    private static bool IsAvailable(WikiPage page)
    {
        if (page.Missing || page.Invalid || page.Redirect)
        {
            return false;
        }

        if (page.Ns != ArticleNamespace)
        {
            return false;
        }

        return page.PageProps?.ContainsKey(DisambiguationProperty) != true;
    }

    private static string? GetImageUrl(WikiPage page)
    {
        return page.Thumbnail == null
            ? null
            : WikiAnimalInformationValidator.ValidateAndGetImageUrl(page.Thumbnail.Source);
    }

    private static MemoryCacheEntryOptions CreateCacheOptions(TimeSpan lifetime)
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime,
            Size = CachedResponseSize
        };
    }
}

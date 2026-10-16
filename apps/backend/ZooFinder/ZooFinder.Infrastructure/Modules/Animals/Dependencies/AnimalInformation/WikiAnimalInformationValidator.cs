using System.Globalization;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Language.Validators;
using ZooFinder.Application.Shared.Pagination.Constants;
using ZooFinder.Application.Shared.Pagination.Contracts;

namespace ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;

internal static class WikiAnimalInformationValidator
{
    private const int MinimumSearchTermLength = 2;
    private const int MaximumSearchTermLength = 200;
    private const int MinimumPageSize = 1;
    private const int ExpectedDetailsPageCount = 1;
    private const string UploadImageHost = "upload.wikimedia.org";
    private const string ThumbnailImageHost = "thumb.wikimedia.org";

    public static void ValidateSearch(string searchTerm, string languageCode, CursorPageRequest pageRequest)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);
        ArgumentNullException.ThrowIfNull(pageRequest);
        LanguageCodeValidator.Validate(languageCode);

        if (searchTerm.Length < MinimumSearchTermLength || searchTerm.Length > MaximumSearchTermLength)
        {
            throw new RequestValidationException(
                $"Wikipedia search term length must be between {MinimumSearchTermLength} and {MaximumSearchTermLength}.");
        }

        if (pageRequest.Limit < MinimumPageSize || pageRequest.Limit > PaginationDefaults.MaximumPageSize)
        {
            throw new RequestValidationException("Wikipedia page size is out of range.");
        }
    }

    public static long ValidateAndGetPageId(string sourceItemId)
    {
        if (!long.TryParse(sourceItemId, NumberStyles.None, CultureInfo.InvariantCulture, out long pageId) || pageId <= 0)
        {
            throw new RequestValidationException("Wikipedia source item ID must be a positive canonical page ID.");
        }

        if (pageId.ToString(CultureInfo.InvariantCulture) != sourceItemId)
        {
            throw new RequestValidationException("Wikipedia source item ID must be a positive canonical page ID.");
        }

        return pageId;
    }

    public static IReadOnlyList<WikiPage> ValidateAndGetSearchPages(WikiApiResponse response, int limit)
    {
        if (response.Query == null)
        {
            if (response.BatchComplete != true)
            {
                throw new InvalidOperationException("Wikipedia returned an invalid search result.");
            }

            // An empty search legitimately omits the query object.
            return Array.Empty<WikiPage>();
        }

        IReadOnlyList<WikiPage> pages = response.Query.Pages
            ?? throw new InvalidOperationException("Wikipedia returned no search page collection.");
        if (pages.Count > limit)
        {
            throw new InvalidOperationException("Wikipedia returned too many search pages.");
        }

        var pageIds = new HashSet<long>();
        foreach (WikiPage page in pages)
        {
            ValidatePage(page);
            ValidateSearchIndex(page.Index);

            if (!pageIds.Add(page.PageId))
            {
                throw new InvalidOperationException("Wikipedia returned duplicate search pages.");
            }
        }

        return pages;
    }

    public static WikiPage ValidateAndGetDetailsPage(WikiApiResponse response, long expectedPageId)
    {
        IReadOnlyList<WikiPage> pages = response.Query?.Pages
            ?? throw new InvalidOperationException("Wikipedia returned no page details.");
        if (pages.Count != ExpectedDetailsPageCount)
        {
            throw new InvalidOperationException("Wikipedia returned an invalid number of detail pages.");
        }

        if (response.Continuation != null)
        {
            throw new InvalidOperationException("Wikipedia returned unexpected detail continuation data.");
        }

        WikiPage page = pages[0];
        ValidatePage(page);

        if (page.PageId != expectedPageId)
        {
            throw new InvalidOperationException("Wikipedia returned a different page identity.");
        }

        return page;
    }

    public static string ValidateAndGetImageUrl(string? source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out Uri? uri))
        {
            throw new InvalidOperationException("Wikipedia returned an invalid thumbnail URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length != 0)
        {
            throw new InvalidOperationException("Wikipedia returned an invalid thumbnail URL.");
        }

        if (uri.Host != UploadImageHost && uri.Host != ThumbnailImageHost)
        {
            throw new InvalidOperationException("Wikipedia returned an unexpected thumbnail host.");
        }

        return uri.AbsoluteUri;
    }

    private static void ValidateSearchIndex(int? index)
    {
        if (index == null || index < 0)
        {
            throw new InvalidOperationException("Wikipedia returned invalid search ordering.");
        }
    }

    private static void ValidatePage(WikiPage? page)
    {
        if (page == null || page.PageId <= 0)
        {
            throw new InvalidOperationException("Wikipedia returned an invalid page identity.");
        }

        if (page.Missing || page.Invalid)
        {
            return;
        }

        if (page.Ns == null || string.IsNullOrWhiteSpace(page.Title))
        {
            throw new InvalidOperationException("Wikipedia returned incomplete page information.");
        }
    }
}

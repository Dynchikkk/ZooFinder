using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZooFinder.Infrastructure.Modules.Animals.Dependencies.AnimalInformation;

internal sealed record WikiApiResponse(
    bool? BatchComplete,
    WikiQuery? Query,
    [property: JsonPropertyName("continue")] WikiContinuation? Continuation,
    WikiApiError? Error);

internal sealed record WikiQuery(IReadOnlyList<WikiPage>? Pages);

internal sealed record WikiPage(
    long PageId,
    int? Ns,
    string? Title,
    int? Index,
    bool Missing,
    bool Invalid,
    bool Redirect,
    string? Extract,
    WikiThumbnail? Thumbnail,
    IReadOnlyDictionary<string, JsonElement>? PageProps);

internal sealed record WikiThumbnail(string? Source);

internal sealed record WikiContinuation(
    [property: JsonPropertyName("gsroffset")] int? SearchOffset,
    [property: JsonPropertyName("continue")] string? Token);

internal sealed record WikiApiError(string? Code);

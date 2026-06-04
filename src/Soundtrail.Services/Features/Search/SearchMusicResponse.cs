using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Features.Search;

public sealed record SearchMusicResponse(
    ResolutionStatus Status,
    string Source,
    SearchQuery Query,
    IReadOnlyList<SearchResult> Results,
    int? RetryAfterSeconds)
{
    public static SearchMusicResponse Resolved(
        SearchQuery query,
        IReadOnlyList<SearchResult> results,
        string source = "local") =>
        new(ResolutionStatus.Resolved,
            source,
            query,
            results,
            RetryAfterSeconds: null);

    public static SearchMusicResponse Pending(
        SearchQuery query,
        int retryAfterSeconds = 60) =>
        new(
            ResolutionStatus.Pending,
            "local",
            query,
            Array.Empty<SearchResult>(),
            retryAfterSeconds);
}

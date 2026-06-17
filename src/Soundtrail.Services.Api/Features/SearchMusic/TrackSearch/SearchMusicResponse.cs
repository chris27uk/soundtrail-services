namespace Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;

public sealed record SearchMusicResponse(
    ResolutionStatus Status,
    string Source,
    string Query,
    IReadOnlyList<SearchResult> Results,
    int? RetryAfterSeconds)
{
    public static SearchMusicResponse Resolved(
        string query,
        IReadOnlyList<SearchResult> results,
        string source = "local") =>
        new(ResolutionStatus.Resolved,
            source,
            query,
            results,
            RetryAfterSeconds: null);

    public static SearchMusicResponse Pending(
        string query,
        int retryAfterSeconds = 60) =>
        new(
            ResolutionStatus.Pending,
            "local",
            query,
            Array.Empty<SearchResult>(),
            retryAfterSeconds);
}

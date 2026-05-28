using MusicResolver.Domain.Tracks;
using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Application.Search;

public sealed record SearchMusicResponse(
    ResolutionStatus Status,
    string Source,
    SearchQuery Query,
    IReadOnlyList<SearchResult> Results,
    QueryId? QueryId,
    int? RetryAfterSeconds)
{
    public static SearchMusicResponse Resolved(
        SearchQuery query,
        IReadOnlyList<SearchResult> results,
        string source = "local") =>
        new(
            ResolutionStatus.Resolved,
            source,
            query,
            results,
            QueryId: null,
            RetryAfterSeconds: null);

    public static SearchMusicResponse Pending(
        SearchQuery query,
        QueryId queryId,
        int retryAfterSeconds = 60) =>
        new(
            ResolutionStatus.Pending,
            "local",
            query,
            Array.Empty<SearchResult>(),
            queryId,
            retryAfterSeconds);
}

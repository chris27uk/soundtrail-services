using MusicResolver.Application.Search;
using MusicResolver.Domain.Tracks;
using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/search",
            async (
                string? q,
                int? limit,
                SearchMusicHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return Results.BadRequest(new { error = "Query parameter 'q' is required." });
                }

                SearchMusicRequest request;

                try
                {
                    request = new SearchMusicRequest(
                        SearchQuery.From(q),
                        Limit.From(limit));
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }

                var response = await handler.Handle(request, cancellationToken);
                return Results.Ok(ToContract(response));
            });

        return endpoints;
    }

    private static object ToContract(SearchMusicResponse response) =>
        response.Status switch
        {
            ResolutionStatus.Resolved => new
            {
                status = "resolved",
                source = response.Source,
                query = response.Query.Value,
                results = response.Results.Select(ToContract)
            },
            _ => new
            {
                status = "pending",
                source = response.Source,
                query = response.Query.Value,
                queryId = response.QueryId!.Value,
                retryAfterSeconds = response.RetryAfterSeconds,
                results = Array.Empty<object>()
            }
        };

    private static object ToContract(MusicResolver.Domain.Tracks.SearchResult result) => new
    {
        title = result.Title.Value,
        artist = result.Artist.Value,
        isrc = result.Isrc?.Value,
        mbid = result.Mbid?.Value,
        appleId = result.AppleId?.Value,
        spotifyId = result.SpotifyId?.Value,
        confidence = result.Confidence.Value
    };
}

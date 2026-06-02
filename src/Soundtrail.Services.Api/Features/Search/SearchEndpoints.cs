using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Api.Features.Search;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/search",
            async (
                string? q,
                int? limit,
                double? minConfidence,
                SearchMusicHandler handler,
                CancellationToken cancellationToken) =>
            {
                SearchMusicRequest request;

                try
                {
                    request = new SearchMusicRequest(
                        SearchQuery.From(q),
                        Limit.From(limit),
                        minConfidence is null ? null : ConfidenceScore.From(minConfidence.Value));
                }
                catch (Exception ex) when (
                    ex is ArgumentException or ArgumentOutOfRangeException)
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
                retryAfterSeconds = response.RetryAfterSeconds,
                results = Array.Empty<object>()
            }
        };

    private static object ToContract(SearchResult result) => new
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

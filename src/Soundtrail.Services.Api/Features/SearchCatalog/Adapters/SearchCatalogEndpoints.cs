using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Api.Features.SearchCatalog.Adapters;

public static class SearchCatalogEndpoints
{
    public static IEndpointRouteBuilder MapSearchCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/search",
            async (
                string? q,
                string? types,
                string? playback,
                int? limit,
                int? offset,
                IApiHandler<SearchCatalogCommand, SearchCatalogResponse> handler,
                CancellationToken cancellationToken) =>
            {
                SearchCatalogCommand request;

                try
                {
                    request = new SearchCatalogCommand(
                        MusicIdentityText.NormalizeFreeText(q ?? throw new ArgumentException("Query is required.", nameof(q))),
                        SearchTypesFilter.Parse(types),
                        PlaybackProviderFilter.Parse(playback),
                        SearchLimit.From(limit),
                        SearchOffset.From(offset));
                }
                catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }

                var response = await handler.Handle(request, cancellationToken);
                return Results.Ok(ToContract(response));
            });

        return endpoints;
    }

    private static object ToContract(SearchCatalogResponse response) => new
    {
        query = response.Query,
        results = response.Results.Select(ToContract),
        discovery = new
        {
            willBeLookedUp = response.Discovery.WillBeLookedUp,
            reason = response.Discovery.Reason,
            retryAfterSeconds = response.Discovery.RetryAfterSeconds
        }
    };

    private static object ToContract(SearchCatalogResult result) => new
    {
        type = ToContract(result.Type),
        id = result.Id,
        name = result.Name,
        artistId = result.ArtistId,
        artistName = result.ArtistName,
        albumId = result.AlbumId,
        albumName = result.AlbumName,
        playabilityStatus = result.PlayabilityStatus.ToString(),
        availableProviders = result.AvailableProviders.Select(ToContract),
        terminallyUnavailableProviders = result.TerminallyUnavailableProviders.Select(ToContract),
        providerReferences = result.ProviderReferences.Select(ToContract)
    };

    private static object ToContract(ProviderReference response) => new
    {
        provider = ToContract(response.Provider),
        providerEntityType = response.ProviderEntityType,
        providerId = response.ProviderId,
        url = response.Url,
        discoveredAt = response.DiscoveredAt
    };

    private static string ToContract(ProviderName provider) =>
        provider.Value switch
        {
            "Spotify" => "spotify",
            "AppleMusic" => "appleMusic",
            "YoutubeMusic" => "youtubeMusic",
            _ => provider.Value
        };

    private static string ToContract(SearchResultType type) =>
        type switch
        {
            SearchResultType.Artist => "artist",
            SearchResultType.Album => "album",
            SearchResultType.Track => "track",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}

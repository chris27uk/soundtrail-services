using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.ListTracksByArtist.Adapters;

public static class ListTracksByArtistEndpoints
{
    public static IEndpointRouteBuilder MapListTracksByArtistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/tracks",
            async (string artistId, IHandler<ListTracksByArtistCommand, ArtistTracksResponse?> handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.Handle(new ListTracksByArtistCommand(ArtistId.From(artistId)), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(ToContract(response));
            });

        return endpoints;
    }

    private static object ToContract(ArtistTracksResponse response) => new
    {
        artistId = response.ArtistId.Value,
        artistName = response.ArtistName,
        tracks = response.Tracks.Select(ToContract)
    };

    private static object ToContract(TrackSummary track) => new
    {
        id = track.TrackId.Value,
        title = track.Title,
        albumId = track.AlbumId.Value,
        albumName = track.AlbumName,
        isrc = track.Isrc,
        durationMs = track.DurationMs,
        playabilityStatus = track.PlayabilityStatus.ToString(),
        availableProviders = track.AvailableProviders.Select(providerName => providerName.ToPersistentId()),
        terminallyUnavailableProviders = track.TerminallyUnavailableProviders.Select(providerName => providerName.ToPersistentId()),
        providerReferences = track.ProviderReferences.Select(ToContract)
    };

    private static object ToContract(ProviderReference response) => new
    {
        provider = response.Provider.ToPersistentId(),
        providerEntityType = response.ProviderEntityType,
        providerId = response.ProviderId,
        url = response.Url,
        discoveredAt = response.DiscoveredAt
    };
}

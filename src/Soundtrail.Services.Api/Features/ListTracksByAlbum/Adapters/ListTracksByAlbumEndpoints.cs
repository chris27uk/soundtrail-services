using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.ListTracksByAlbum.Adapters;

public static class ListTracksByAlbumEndpoints
{
    public static IEndpointRouteBuilder MapListTracksByAlbumEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}/tracks",
            async (string artistId, string albumId, IHandler<ListTracksByAlbumCommand, AlbumTracksResponse?> handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.Handle(new ListTracksByAlbumCommand(ArtistId.From(artistId), AlbumId.From(albumId)), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(ToContract(response));
            });

        return endpoints;
    }

    private static object ToContract(AlbumTracksResponse response) => new
    {
        artistId = response.ArtistId.Value,
        artistName = response.ArtistName,
        albumId = response.AlbumId.Value,
        albumName = response.AlbumName,
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

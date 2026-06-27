using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.RequestKnownCatalogItem;

namespace Soundtrail.Services.Api.Features.ListTracksByAlbum.Adapters;

public static class ListTracksByAlbumEndpoints
{
    public static IEndpointRouteBuilder MapListTracksByAlbumEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}/tracks",
            async (string artistId, string albumId, string? playback, IApiHandler<ListTracksByAlbumCommand, AlbumTracksResponse?> handler, RequestKnownCatalogItemHandler requestHandler, CancellationToken cancellationToken) =>
            {
                var providerFilter = PlaybackProviderFilter.Parse(playback);
                var artist = ArtistId.From(artistId);
                var album = AlbumId.From(albumId);
                var response = await handler.Handle(new ListTracksByAlbumCommand(artist, album), cancellationToken);
                await requestHandler.Handle(
                    new KnownCatalogItemRequested(
                        KnownCatalogItem.ForAlbum(album),
                        providerFilter,
                        0,
                        0,
                        DateTimeOffset.UtcNow,
                        CorrelationId.New()),
                    cancellationToken);
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

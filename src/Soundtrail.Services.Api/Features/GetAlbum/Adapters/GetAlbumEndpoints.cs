using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;

namespace Soundtrail.Services.Api.Features.GetAlbum.Adapters;

public static class GetAlbumEndpoints
{
    public static IEndpointRouteBuilder MapGetAlbumEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}",
            async (string artistId, string albumId, IApiHandler<GetAlbumCommand, AlbumDetailsResponse?> handler, CancellationToken cancellationToken) =>
            {
                var artist = ArtistId.From(artistId);
                var album = AlbumId.From(albumId);
                var response = await handler.Handle(new GetAlbumCommand(artist, album), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(ToContract(response));
            });

        return endpoints;
    }

    private static object ToContract(AlbumDetailsResponse response) => new
    {
        artistId = response.ArtistId.Value,
        artistName = response.ArtistName,
        id = response.AlbumId.Value,
        name = response.Name,
        releaseDate = response.ReleaseDate,
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

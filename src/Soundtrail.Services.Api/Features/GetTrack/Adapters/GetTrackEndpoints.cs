using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Api.Features.GetTrack.Adapters;

public static class GetTrackEndpoints
{
    public static IEndpointRouteBuilder MapGetTrackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}/tracks/{trackId}",
            async (string artistId, string albumId, string trackId, string? playback, IApiHandler<GetTrackCommand, TrackDetailsResponse?> handler, CancellationToken cancellationToken) =>
            {
                var providerFilter = PlaybackProviderFilter.Parse(playback);
                var artist = ArtistId.From(artistId);
                var album = AlbumId.From(albumId);
                var track = TrackId.From(trackId);
                var response = await handler.Handle(
                    new GetTrackCommand(artist, album, track, providerFilter),
                    cancellationToken);

                return response is null ? Results.NotFound() : Results.Ok(ToContract(response));
            });

        return endpoints;
    }

    private static object ToContract(TrackDetailsResponse response) => new
    {
        artistId = response.ArtistId.Value,
        artistName = response.ArtistName,
        albumId = response.AlbumId.Value,
        albumName = response.AlbumName,
        id = response.TrackId.Value,
        title = response.Title,
        isrc = response.Isrc,
        durationMs = response.DurationMs,
        playabilityStatus = response.PlayabilityStatus.ToString(),
        availableProviders = response.AvailableProviders.Select(providerName => providerName.ToPersistentId()),
        terminallyUnavailableProviders = response.TerminallyUnavailableProviders.Select(providerName => providerName.ToPersistentId()),
        providerReferences = response.ProviderReferences.Select(ToContract)
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

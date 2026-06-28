using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

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

                return response is null ? Results.NotFound() : Results.Ok(TypeTranslationRegistry.Default.Translate<Soundtrail.Contracts.Api.TrackDetailsResponseDto>(response));
            });

        return endpoints;
    }
}

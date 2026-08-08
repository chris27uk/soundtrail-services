using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;

internal sealed class ReadPlaylistTracksByProviderPortFake : IReadPlaylistTracksByProviderPort
{
    private readonly Dictionary<(PlaylistId PlaylistId, ProviderName Provider), IReadOnlyList<TrackReference>> tracks = [];
    private readonly Exception? failure;

    private ReadPlaylistTracksByProviderPortFake(Exception? failure) => this.failure = failure;

    public ReadPlaylistTracksByProviderPortFake() : this(failure: null)
    {
    }

    public static ReadPlaylistTracksByProviderPortFake Empty() => new();

    public static ReadPlaylistTracksByProviderPortFake ThatThrows(Exception error) =>
        new(failure: error);

    public ReadPlaylistTracksByProviderPortFake WithTracks(
        PlaylistId playlistId,
        ProviderName provider,
        params TrackReference[] playlistTracks)
    {
        tracks[(playlistId, provider)] = playlistTracks;
        return this;
    }

    public Task<IReadOnlyList<TrackReference>> ReadAsync(
        PlaylistId playlistId,
        ProviderName provider,
        CancellationToken cancellationToken) =>
        failure is not null
            ? Task.FromException<IReadOnlyList<TrackReference>>(failure)
            : Task.FromResult(tracks.GetValueOrDefault((playlistId, provider), []));
}

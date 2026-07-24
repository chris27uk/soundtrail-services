using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

public sealed class RavenStorePlaylistTracksReadModelPort(IDocumentStore documentStore) : IStorePlaylistTracksReadModelPort
{
    public async Task StoreAsync(PlaylistTracksDiscovered @event, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var trackIdValues = @event.Tracks.Select(static trackId => trackId.Value).ToArray();
        var record = await BuildRecordAsync(session, @event.PlaylistId.Value, trackIdValues, @event.ObservedAt, cancellationToken);

        await session.StoreAsync(record, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task RepairTrackAsync(TrackId trackId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var playlistRecords = await session.Query<CatalogPlaylistTracksRecordDto>()
            .ToListAsync(cancellationToken);
        var affectedRecords = playlistRecords
            .Where(record => ContainsSameBaseTrack(record, trackId))
            .ToArray();

        if (affectedRecords.Length == 0)
        {
            return;
        }

        foreach (var existingRecord in affectedRecords)
        {
            var rebuilt = await BuildRecordAsync(
                session,
                existingRecord.PlaylistId,
                existingRecord.TrackIds,
                existingRecord.UpdatedAt,
                cancellationToken);

            existingRecord.Tracks = rebuilt.Tracks;
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    private static async Task<CatalogPlaylistTracksRecordDto> BuildRecordAsync(
        IAsyncDocumentSession session,
        string playlistId,
        IReadOnlyList<string> trackIds,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var playlistTrackIds = trackIds
            .Select(TrackId.From)
            .ToArray();
        var requestedBases = playlistTrackIds
            .Select(static trackId => trackId.BaseComponent)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var siblingTracks = await session.Query<CatalogTrackRecordDto>()
            .ToListAsync(cancellationToken);
        var tracksByBase = siblingTracks
            .Select(track => (Track: track, TrackId: TrackId.From(track.TrackId)))
            .Where(entry => requestedBases.Contains(entry.TrackId.BaseComponent))
            .GroupBy(entry => entry.TrackId.BaseComponent, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static entry => entry.Track).ToArray(),
                StringComparer.Ordinal);

        return new CatalogPlaylistTracksRecordDto
        {
            Id = CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId),
            PlaylistId = playlistId,
            TrackIds = trackIds.ToArray(),
            Tracks = trackIds
                .Select(TrackId.From)
                .Select(trackId => SelectPreferredTrack(tracksByBase, trackId))
                .Where(static track => track is not null)
                .Select(track => new CatalogPlaylistTrackRecordDto
                {
                    TrackId = track!.TrackId,
                    MusicCatalogId = track.MusicCatalogId,
                    Title = track.Title,
                    ArtistName = track.ArtistName,
                    AlbumTitle = track.AlbumTitle,
                    DurationMs = track.DurationMs,
                    Isrc = track.Isrc,
                    ReleaseDate = track.ReleaseDate,
                    ReleaseType = track.ReleaseType,
                    ArtworkUrl = track.ArtworkUrl
                })
                .ToArray(),
            UpdatedAt = updatedAt
        };
    }

    private static CatalogTrackRecordDto? SelectPreferredTrack(
        IReadOnlyDictionary<string, CatalogTrackRecordDto[]> tracksByBase,
        TrackId requestedTrackId)
    {
        if (!tracksByBase.TryGetValue(requestedTrackId.BaseComponent, out var candidates))
        {
            return null;
        }

        return candidates.FirstOrDefault(track => string.Equals(track.TrackId, requestedTrackId.Value, StringComparison.Ordinal))
            ?? candidates.OrderByDescending(static track => track.UpdatedAt).FirstOrDefault();
    }

    private static bool ContainsSameBaseTrack(CatalogPlaylistTracksRecordDto record, TrackId trackId) =>
        record.TrackIds
            .Select(TrackId.From)
            .Any(existingTrackId => existingTrackId.SharesBaseWith(trackId));
}

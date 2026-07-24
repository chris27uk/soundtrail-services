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
            .Select(TrackIdIndexProjection.From)
            .DistinctBy(static projection => (projection.BaseHigh, projection.BaseLow))
            .ToArray();
        var siblingTracks = await session.Query<CatalogTrackRecordDto>()
            .ToListAsync(cancellationToken);
        var tracksByBase = siblingTracks
            .Select(track =>
            {
                var trackId = TrackId.From(track.TrackId);
                return (Track: track, Projection: TrackIdIndexProjection.From(trackId));
            })
            .Where(entry => requestedBases.Any(requested => requested.SharesBaseWith(entry.Projection)))
            .GroupBy(entry => (entry.Projection.BaseHigh, entry.Projection.BaseLow))
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static entry => entry.Track).ToArray());

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
        IReadOnlyDictionary<(ulong BaseHigh, ulong BaseLow), CatalogTrackRecordDto[]> tracksByBase,
        TrackId requestedTrackId)
    {
        var requestedProjection = TrackIdIndexProjection.From(requestedTrackId);
        if (!tracksByBase.TryGetValue((requestedProjection.BaseHigh, requestedProjection.BaseLow), out var candidates))
        {
            return null;
        }

        return candidates.FirstOrDefault(track => string.Equals(track.TrackId, requestedTrackId.Value, StringComparison.Ordinal))
            ?? candidates
                .Select(track => (Track: track, Projection: TrackIdIndexProjection.From(TrackId.From(track.TrackId))))
                .OrderBy(entry => entry.Projection.GetDistanceTo(requestedProjection))
                .ThenByDescending(static entry => entry.Track.UpdatedAt)
                .Select(static entry => entry.Track)
                .FirstOrDefault();
    }

    private static bool ContainsSameBaseTrack(CatalogPlaylistTracksRecordDto record, TrackId trackId)
    {
        var requestedProjection = TrackIdIndexProjection.From(trackId);
        return record.TrackIds
            .Select(TrackId.From)
            .Select(TrackIdIndexProjection.From)
            .Any(existingProjection => existingProjection.SharesBaseWith(requestedProjection));
    }
}

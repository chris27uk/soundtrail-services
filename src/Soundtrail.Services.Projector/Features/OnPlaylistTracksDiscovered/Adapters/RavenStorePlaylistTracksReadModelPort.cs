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
        var documentId = CatalogPlaylistTracksRecordDto.GetDocumentId(@event.PlaylistId.Value);
        var existingRecord = await session.LoadAsync<CatalogPlaylistTracksRecordDto>(documentId, cancellationToken);
        var trackIdValues = MergeTrackIds(
            existingRecord?.TrackIds,
            @event.Tracks.Select(static trackId => trackId.Value));
        var record = await BuildRecordAsync(session, @event.PlaylistId.Value, trackIdValues, @event.ObservedAt, cancellationToken);

        if (existingRecord is null)
        {
            await session.StoreAsync(record, cancellationToken);
        }
        else
        {
            existingRecord.TrackIds = record.TrackIds;
            existingRecord.Tracks = record.Tracks;
            existingRecord.Discovery = record.Discovery ?? existingRecord.Discovery;
            existingRecord.UpdatedAt = record.UpdatedAt;
        }

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
            existingRecord.Discovery = rebuilt.Discovery ?? existingRecord.Discovery;
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

        var record = new CatalogPlaylistTracksRecordDto
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
                    ArtworkUrl = track.ArtworkUrl,
                    StreamingLocations = track.StreamingLocations
                })
                .ToArray(),
            UpdatedAt = updatedAt
        };

        record.Discovery = await LoadDiscoveryAsync(session, record, cancellationToken);
        return record;
    }

    private static async Task<CatalogDiscoveryFeedbackRecordDto?> LoadDiscoveryAsync(
        IAsyncDocumentSession session,
        CatalogPlaylistTracksRecordDto playlist,
        CancellationToken cancellationToken)
    {
        var targetId = $"child_tracks_for_playlist:{playlist.PlaylistId}";
        var playlistDiscovery = await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(
            CatalogDiscoveryFeedbackRecordDto.GetDocumentId(targetId),
            cancellationToken);

        if (playlistDiscovery is null)
        {
            return null;
        }

        foreach (var track in playlist.Tracks.Where(static track => track.StreamingLocations.Length == 0))
        {
            var streamingTargetId = $"streaming_location_for_track:{track.TrackId}";
            var streamingDiscovery = await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(
                CatalogDiscoveryFeedbackRecordDto.GetDocumentId(streamingTargetId),
                cancellationToken);

            if (streamingDiscovery is null || IsIncomplete(streamingDiscovery))
            {
                return streamingDiscovery is null
                    ? BuildStreamingProjectionPendingDiscovery(playlistDiscovery)
                    : EmbedDiscovery(streamingDiscovery);
            }
        }

        return EmbedDiscovery(playlistDiscovery);
    }

    private static bool IsIncomplete(CatalogDiscoveryFeedbackRecordDto discovery) =>
        discovery.Status is "requested" or "scheduled" or "deferred";

    private static CatalogDiscoveryFeedbackRecordDto BuildStreamingProjectionPendingDiscovery(
        CatalogDiscoveryFeedbackRecordDto playlistDiscovery)
    {
        var pending = EmbedDiscovery(playlistDiscovery);
        pending.Status = "scheduled";
        pending.NextEligibleAtUtc = playlistDiscovery.UpdatedAtUtc.AddSeconds(15);
        pending.EarliestExpectedCompletionAtUtc = playlistDiscovery.UpdatedAtUtc.AddSeconds(75);
        pending.Reason = "Track streaming projection is still catching up.";
        return pending;
    }

    private static CatalogDiscoveryFeedbackRecordDto EmbedDiscovery(CatalogDiscoveryFeedbackRecordDto discovery) =>
        new()
        {
            TargetId = discovery.TargetId,
            Status = discovery.Status,
            Priority = discovery.Priority,
            NextEligibleAtUtc = discovery.NextEligibleAtUtc,
            EarliestExpectedCompletionAtUtc = discovery.EarliestExpectedCompletionAtUtc,
            Reason = discovery.Reason,
            UpdatedAtUtc = discovery.UpdatedAtUtc
        };

    private static string[] MergeTrackIds(
        IReadOnlyCollection<string>? existingTrackIds,
        IEnumerable<string> discoveredTrackIds)
    {
        if (existingTrackIds is null || existingTrackIds.Count == 0)
        {
            return discoveredTrackIds
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        var mergedTrackIds = new List<string>(existingTrackIds.Count);
        var seenTrackIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var trackId in existingTrackIds)
        {
            if (seenTrackIds.Add(trackId))
            {
                mergedTrackIds.Add(trackId);
            }
        }

        foreach (var trackId in discoveredTrackIds)
        {
            if (seenTrackIds.Add(trackId))
            {
                mergedTrackIds.Add(trackId);
            }
        }

        return mergedTrackIds.ToArray();
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

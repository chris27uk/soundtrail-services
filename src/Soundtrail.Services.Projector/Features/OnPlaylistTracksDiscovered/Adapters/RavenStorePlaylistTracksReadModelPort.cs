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
            .Where(x => x.TrackBaseKeyHighs.Contains(trackId.BaseKeyHigh) && x.TrackBaseKeyLows.Contains(trackId.BaseKeyLow))
            .ToListAsync(cancellationToken);

        if (playlistRecords.Count == 0)
        {
            return;
        }

        foreach (var existingRecord in playlistRecords)
        {
            if (!ContainsBaseKey(existingRecord, trackId))
            {
                continue;
            }

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
        var tracksByBaseKey = new Dictionary<(string High, string Low), CatalogTrackRecordDto>();

        foreach (var group in playlistTrackIds.GroupBy(static x => (x.BaseKeyHigh, x.BaseKeyLow)))
        {
            var sample = group.First();
            var siblingTracks = await session.Query<CatalogTrackRecordDto>()
                .Where(x => x.TrackIdBaseKeyHigh == sample.BaseKeyHigh && x.TrackIdBaseKeyLow == sample.BaseKeyLow)
                .ToListAsync(cancellationToken);

            var preferredTrack = siblingTracks.FirstOrDefault(x => group.Any(entry => entry.Value == x.TrackId))
                ?? siblingTracks.OrderByDescending(static x => x.UpdatedAt).FirstOrDefault();

            if (preferredTrack is not null)
            {
                tracksByBaseKey[(sample.BaseKeyHigh, sample.BaseKeyLow)] = preferredTrack;
            }
        }

        return new CatalogPlaylistTracksRecordDto
        {
            Id = CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId),
            PlaylistId = playlistId,
            TrackIds = trackIds.ToArray(),
            TrackBaseKeyHighs = playlistTrackIds.Select(static x => x.BaseKeyHigh).ToArray(),
            TrackBaseKeyLows = playlistTrackIds.Select(static x => x.BaseKeyLow).ToArray(),
            Tracks = trackIds
                .Select(TrackId.From)
                .Select(trackId => tracksByBaseKey.GetValueOrDefault((trackId.BaseKeyHigh, trackId.BaseKeyLow)))
                .Where(static track => track is not null)
                .Select(track => new CatalogPlaylistTrackRecordDto
                {
                    TrackId = track!.TrackId,
                    TrackIdBaseKeyHigh = track.TrackIdBaseKeyHigh,
                    TrackIdBaseKeyLow = track.TrackIdBaseKeyLow,
                    TrackIdSpecificKey = track.TrackIdSpecificKey,
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

    private static bool ContainsBaseKey(CatalogPlaylistTracksRecordDto record, TrackId trackId)
    {
        for (var index = 0; index < record.TrackBaseKeyHighs.Length && index < record.TrackBaseKeyLows.Length; index++)
        {
            if (record.TrackBaseKeyHighs[index] == trackId.BaseKeyHigh
                && record.TrackBaseKeyLows[index] == trackId.BaseKeyLow)
            {
                return true;
            }
        }

        return false;
    }
}

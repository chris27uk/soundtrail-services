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
            .Where(x => x.TrackIds.Contains(trackId.Value))
            .ToListAsync(cancellationToken);

        if (playlistRecords.Count == 0)
        {
            return;
        }

        foreach (var existingRecord in playlistRecords)
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
        var trackDocumentIds = trackIds.Select(CatalogTrackRecordDto.GetDocumentId).ToArray();
        var trackDocuments = await session.LoadAsync<CatalogTrackRecordDto>(trackDocumentIds, cancellationToken);

        return new CatalogPlaylistTracksRecordDto
        {
            Id = CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId),
            PlaylistId = playlistId,
            TrackIds = trackIds.ToArray(),
            Tracks = trackIds
                .Select(trackId => trackDocuments.TryGetValue(CatalogTrackRecordDto.GetDocumentId(trackId), out var track) ? track : null)
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
}

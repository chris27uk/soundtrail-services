using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogTrackImportWriter(
    IEventStreamRepository<ArtistId> artistRepository,
    IDocumentStore documentStore) : ICatalogTrackImportWriter
{
    public async Task WriteAsync(
        Track track,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(track);

        if (string.IsNullOrWhiteSpace(track.AlbumId))
        {
            return;
        }

        var albumId = AlbumId.From(track.AlbumId);
        var artistId = ArtistId.From(albumId.ArtistId);
        var (stream, catalog) = await ArtistCatalog.LoadAsync(artistRepository, artistId, cancellationToken);
        var existing = stream.Events
            .OfType<TrackDiscovered>()
            .LastOrDefault(@event => @event.Track.TrackId.Value == track.TrackId.Value);
        if (existing is not null && existing.ObservedAt >= dumpObservedAt)
        {
            return;
        }

        var trackToWrite = new Track(track.TrackId)
        {
            Title = track.Title,
            ArtistName = track.ArtistName,
            AlbumTitle = track.AlbumTitle,
            AlbumId = track.AlbumId,
            DurationMs = track.DurationMs,
            Isrc = track.Isrc,
            ReleaseDate = track.ReleaseDate,
            ReleaseType = track.ReleaseType,
            ArtworkUrl = track.ArtworkUrl,
            UpdatedAt = dumpObservedAt
        };
        SourceSystemIdSet.UnionWith(trackToWrite.SourceSystemIds, track.SourceSystemIds);

        catalog.CatalogItemDiscovered(new CatalogItem.MusicTrack(trackToWrite));
        await catalog.SaveAsync(
            artistRepository,
            stream,
            MessageId.For($"bulk-import:TrackDiscovered:{track.TrackId.Value}:{dumpObservedAt:O}"),
            cancellationToken,
            ProjectionHint.BulkImport);

        await StoreTrackReadModelAsync(artistId, trackToWrite, dumpObservedAt, cancellationToken);
    }

    private async Task StoreTrackReadModelAsync(
        ArtistId artistId,
        Track track,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(
            new CatalogTrackRecordDto
            {
                Id = CatalogTrackRecordDto.GetDocumentId(track.TrackId.Value),
                TrackId = track.TrackId.Value,
                MusicCatalogId = track.TrackId.Value,
                ArtistId = artistId.Value,
                Title = track.Title,
                ArtistName = track.ArtistName,
                AlbumTitle = track.AlbumTitle,
                DurationMs = track.DurationMs,
                Isrc = track.Isrc,
                ReleaseDate = track.ReleaseDate,
                ReleaseType = track.ReleaseType,
                ArtworkUrl = track.ArtworkUrl,
                StreamingLocations = [],
                UpdatedAt = updatedAt
            },
            cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}

using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

/// <summary>
/// Sociable batch writer that preserves per-entity fake import captures.
/// </summary>
internal sealed class CatalogDumpBatchWriterFake(
    ICatalogArtistImportWriter artistWriter,
    ICatalogAlbumImportWriter albumWriter,
    ICatalogTrackImportWriter trackWriter) : ICatalogDumpBatchWriter
{
    public async Task<IReadOnlySet<ArtistId>> AppendEventsAsync(
        IReadOnlyList<CatalogDumpBatchItem> items,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var touched = new HashSet<ArtistId>();
        foreach (var item in items)
        {
            switch (item)
            {
                case ArtistDumpBatchItem(var artist):
                    await artistWriter.WriteAsync(artist, dumpObservedAt, cancellationToken);
                    touched.Add(artist.Id);
                    break;
                case AlbumDumpBatchItem(var album):
                    await albumWriter.WriteAsync(album, dumpObservedAt, cancellationToken);
                    touched.Add(ArtistId.From(album.AlbumId.ArtistId));
                    break;
                case TrackDumpBatchItem(var track):
                    await trackWriter.WriteAsync(track, dumpObservedAt, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(track.AlbumId))
                    {
                        touched.Add(ArtistId.From(AlbumId.From(track.AlbumId).ArtistId));
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Unsupported dump batch item '{item.GetType().Name}'.");
            }
        }

        return touched;
    }

    public Task ProjectArtistsAsync(
        IReadOnlySet<ArtistId> artistIds,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

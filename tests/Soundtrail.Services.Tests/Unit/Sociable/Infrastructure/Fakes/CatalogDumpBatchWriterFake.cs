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
    public async Task FlushAsync(
        IReadOnlyList<CatalogDumpBatchItem> items,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            switch (item)
            {
                case ArtistDumpBatchItem(var artist):
                    await artistWriter.WriteAsync(artist, dumpObservedAt, cancellationToken);
                    break;
                case AlbumDumpBatchItem(var album):
                    await albumWriter.WriteAsync(album, dumpObservedAt, cancellationToken);
                    break;
                case TrackDumpBatchItem(var track):
                    await trackWriter.WriteAsync(track, dumpObservedAt, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported dump batch item '{item.GetType().Name}'.");
            }
        }
    }
}

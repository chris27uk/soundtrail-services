using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogTrackImportWriter(ICatalogDumpBatchWriter batchWriter) : ICatalogTrackImportWriter
{
    public Task WriteAsync(
        Track track,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(track);
        return batchWriter.FlushAsync(
            [new TrackDumpBatchItem(track)],
            dumpObservedAt,
            cancellationToken);
    }
}

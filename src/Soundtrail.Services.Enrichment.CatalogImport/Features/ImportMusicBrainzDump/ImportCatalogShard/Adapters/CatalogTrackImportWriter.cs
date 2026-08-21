using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogTrackImportWriter(ICatalogDumpBatchWriter batchWriter) : ICatalogTrackImportWriter
{
    public async Task WriteAsync(
        Track track,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(track);
        var touched = await batchWriter.AppendEventsAsync(
            [new TrackDumpBatchItem(track)],
            dumpObservedAt,
            cancellationToken);
        await batchWriter.ProjectArtistsAsync(touched, dumpObservedAt, cancellationToken);
    }
}

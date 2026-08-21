using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogArtistImportWriter(ICatalogDumpBatchWriter batchWriter) : ICatalogArtistImportWriter
{
    public async Task WriteAsync(
        Artist artist,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artist);
        var touched = await batchWriter.AppendEventsAsync(
            [new ArtistDumpBatchItem(artist)],
            dumpObservedAt,
            cancellationToken);
        await batchWriter.ProjectArtistsAsync(touched, dumpObservedAt, cancellationToken);
    }
}

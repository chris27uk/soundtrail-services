using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogArtistImportWriter(ICatalogDumpBatchWriter batchWriter) : ICatalogArtistImportWriter
{
    public Task WriteAsync(
        Artist artist,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artist);
        return batchWriter.FlushAsync(
            [new ArtistDumpBatchItem(artist)],
            dumpObservedAt,
            cancellationToken);
    }
}

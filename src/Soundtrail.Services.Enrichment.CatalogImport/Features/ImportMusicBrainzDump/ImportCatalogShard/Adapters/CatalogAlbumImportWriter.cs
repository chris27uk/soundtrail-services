using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogAlbumImportWriter(ICatalogDumpBatchWriter batchWriter) : ICatalogAlbumImportWriter
{
    public Task WriteAsync(
        Album album,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(album);
        return batchWriter.FlushAsync(
            [new AlbumDumpBatchItem(album)],
            dumpObservedAt,
            cancellationToken);
    }
}

using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogAlbumImportWriter(ICatalogDumpBatchWriter batchWriter) : ICatalogAlbumImportWriter
{
    public async Task WriteAsync(
        Album album,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(album);
        var touched = await batchWriter.AppendEventsAsync(
            [new AlbumDumpBatchItem(album)],
            dumpObservedAt,
            cancellationToken);
        await batchWriter.ProjectArtistsAsync(touched, dumpObservedAt, cancellationToken);
    }
}

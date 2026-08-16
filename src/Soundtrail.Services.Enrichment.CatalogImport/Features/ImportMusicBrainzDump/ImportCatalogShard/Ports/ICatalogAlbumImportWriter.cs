using Soundtrail.Domain.Catalog.Albums;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface ICatalogAlbumImportWriter
{
    Task WriteAsync(Album album, DateTimeOffset dumpObservedAt, CancellationToken cancellationToken = default);
}

using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface ICatalogArtistImportWriter
{
    Task WriteAsync(Artist artist, DateTimeOffset dumpObservedAt, CancellationToken cancellationToken = default);
}

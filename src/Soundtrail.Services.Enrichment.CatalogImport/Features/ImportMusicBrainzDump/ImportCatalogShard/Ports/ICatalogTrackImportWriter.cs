using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface ICatalogTrackImportWriter
{
    Task WriteAsync(Track track, DateTimeOffset dumpObservedAt, CancellationToken cancellationToken = default);
}

using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface IMusicBrainzTrackDumpRowMapper
{
    Track? TryMap(string jsonLine);
}

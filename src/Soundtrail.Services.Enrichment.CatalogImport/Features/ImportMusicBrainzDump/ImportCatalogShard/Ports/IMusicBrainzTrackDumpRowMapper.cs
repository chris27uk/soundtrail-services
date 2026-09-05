using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface IMusicBrainzTrackDumpRowMapper
{
    Track? TryMap(string jsonLine);

    /// <summary>
    /// Reads only the credited artist id from a wrapped shard line (no full track mapping).
    /// </summary>
    bool TryPeekArtistId(string jsonLine, out ArtistId artistId);
}

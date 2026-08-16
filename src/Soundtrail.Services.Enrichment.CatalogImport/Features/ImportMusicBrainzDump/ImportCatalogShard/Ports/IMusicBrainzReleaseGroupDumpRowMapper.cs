using Soundtrail.Domain.Catalog.Albums;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface IMusicBrainzReleaseGroupDumpRowMapper
{
    /// <summary>
    /// Maps a release-group shard JSONL row (with creditedArtistId envelope). Returns null for bad rows.
    /// </summary>
    Album? TryMap(string jsonLine);
}

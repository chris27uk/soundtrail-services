using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface IMusicBrainzArtistDumpRowMapper
{
    /// <summary>
    /// Maps a MusicBrainz artist JSONL row. Returns null for skippable bad rows.
    /// </summary>
    Artist? TryMap(string jsonLine);
}

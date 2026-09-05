using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface IMusicBrainzArtistDumpRowMapper
{
    /// <summary>
    /// Cheap artist-id peek without constructing an <see cref="Artist"/>.
    /// </summary>
    bool TryPeekArtistId(string jsonLine, out ArtistId artistId);

    /// <summary>
    /// Maps a MusicBrainz artist JSONL row. Returns null for skippable bad rows.
    /// </summary>
    Artist? TryMap(string jsonLine);
}

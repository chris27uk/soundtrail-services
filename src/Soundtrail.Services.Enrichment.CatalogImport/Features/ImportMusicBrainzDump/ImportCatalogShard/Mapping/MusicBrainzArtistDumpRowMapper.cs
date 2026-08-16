using System.Text.Json;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Mapping;

public sealed class MusicBrainzArtistDumpRowMapper : IMusicBrainzArtistDumpRowMapper
{
    public Artist? TryMap(string jsonLine)
    {
        if (string.IsNullOrWhiteSpace(jsonLine))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonLine);
            var root = document.RootElement;
            if (!root.TryGetProperty("id", out var idProperty))
            {
                return null;
            }

            var mbid = idProperty.GetString();
            if (string.IsNullOrWhiteSpace(mbid))
            {
                return null;
            }

            var name = root.TryGetProperty("name", out var nameProperty)
                ? nameProperty.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return new Artist
            {
                Id = ArtistId.From(mbid),
                Name = ArtistName.From(name),
                SourceSystemIds = SourceSystemIdSet.FromLegacyMusicBrainz(mbid)
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

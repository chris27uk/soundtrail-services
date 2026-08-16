using System.Text.Json;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;

public sealed class MusicBrainzReleaseGroupDumpRowMapper : IMusicBrainzReleaseGroupDumpRowMapper
{
    public Album? TryMap(string jsonLine)
    {
        if (string.IsNullOrWhiteSpace(jsonLine))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonLine);
            var root = document.RootElement;
            if (!root.TryGetProperty("creditedArtistId", out var creditedArtistProperty) ||
                !root.TryGetProperty("releaseGroup", out var releaseGroup))
            {
                return null;
            }

            var creditedArtistId = creditedArtistProperty.GetString();
            if (string.IsNullOrWhiteSpace(creditedArtistId))
            {
                return null;
            }

            if (!releaseGroup.TryGetProperty("id", out var idProperty))
            {
                return null;
            }

            var releaseGroupId = idProperty.GetString();
            if (string.IsNullOrWhiteSpace(releaseGroupId))
            {
                return null;
            }

            var title = releaseGroup.TryGetProperty("title", out var titleProperty)
                ? titleProperty.GetString()
                : releaseGroup.TryGetProperty("name", out var nameProperty)
                    ? nameProperty.GetString()
                    : null;
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return new Album(
                AlbumId.From(creditedArtistId, releaseGroupId),
                title,
                SourceSystemIdSet.FromLegacyMusicBrainz(releaseGroupId),
                releaseDate: null,
                artworkUrl: null,
                updatedAt: default);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

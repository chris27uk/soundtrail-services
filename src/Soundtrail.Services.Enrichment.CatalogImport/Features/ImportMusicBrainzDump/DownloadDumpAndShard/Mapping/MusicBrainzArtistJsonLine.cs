using System.Text.Json;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;

public static class MusicBrainzArtistJsonLine
{
    public static bool TryReadArtistId(string line, out string artistId)
    {
        artistId = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            if (!document.RootElement.TryGetProperty("id", out var idProperty))
            {
                return false;
            }

            artistId = idProperty.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(artistId);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

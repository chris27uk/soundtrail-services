using System.Text;
using System.Text.Json;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;

public static class MusicBrainzReleaseGroupJsonLine
{
    public static bool TryReadCreditedArtistIds(string line, out IReadOnlyList<string> artistIds)
    {
        artistIds = [];
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("id", out _))
            {
                return false;
            }

            var ids = new List<string>();
            if (root.TryGetProperty("artist-credit", out var credits) &&
                credits.ValueKind == JsonValueKind.Array)
            {
                foreach (var credit in credits.EnumerateArray())
                {
                    if (credit.TryGetProperty("artist", out var artist) &&
                        artist.TryGetProperty("id", out var idProperty))
                    {
                        var id = idProperty.GetString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            ids.Add(id);
                        }
                    }
                }
            }

            artistIds = ids.Distinct(StringComparer.Ordinal).ToArray();
            return artistIds.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string WrapForCreditedArtist(string creditedArtistId, string releaseGroupJsonLine)
    {
        using var releaseGroup = JsonDocument.Parse(releaseGroupJsonLine);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("creditedArtistId", creditedArtistId);
            writer.WritePropertyName("releaseGroup");
            releaseGroup.RootElement.WriteTo(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

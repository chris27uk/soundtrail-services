using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;

public static class MusicBrainzTrackJsonLine
{
    public static bool TryPeekCreditedArtistId(string line, out string artistId)
    {
        artistId = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            if (!document.RootElement.TryGetProperty("creditedArtistId", out var creditedArtistProperty))
            {
                return false;
            }

            artistId = creditedArtistProperty.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(artistId);
        }
        catch (JsonException)
        {
            return false;
        }
    }

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

    public static string WrapForCreditedArtist(string creditedArtistId, string trackJsonLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(creditedArtistId);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackJsonLine);

        var buffer = new ArrayBufferWriter<byte>(trackJsonLine.Length + 64);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("creditedArtistId", creditedArtistId);
            writer.WritePropertyName("track");
            writer.WriteRawValue(trackJsonLine);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}

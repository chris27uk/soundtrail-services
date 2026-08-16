using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;

/// <summary>
/// Materializes Soundtrail denormalized track-graph JSONL from official MusicBrainz release dump lines
/// (release → media → tracks → recording + release-group + date).
/// </summary>
public static class MusicBrainzReleaseGraphTrackJoiner
{
    public static IReadOnlyList<string> JoinReleaseLines(IEnumerable<string> releaseLines)
    {
        ArgumentNullException.ThrowIfNull(releaseLines);

        var bestByKey = new Dictionary<string, Candidate>(StringComparer.Ordinal);
        foreach (var line in releaseLines)
        {
            foreach (var candidate in EnumerateCandidates(line))
            {
                if (!bestByKey.TryGetValue(candidate.Key, out var existing) ||
                    IsPreferable(candidate, existing))
                {
                    bestByKey[candidate.Key] = candidate;
                }
            }
        }

        return bestByKey.Values
            .OrderBy(static c => c.Key, StringComparer.Ordinal)
            .Select(static c => c.JsonLine)
            .ToArray();
    }

    public static async Task WriteJoinedTracksAsync(
        string releaseJsonlPath,
        string trackJsonlPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseJsonlPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackJsonlPath);

        var lines = new List<string>();
        await foreach (var line in File.ReadLinesAsync(releaseJsonlPath, cancellationToken))
        {
            lines.Add(line);
        }

        var joined = JoinReleaseLines(lines);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(trackJsonlPath))!);
        await File.WriteAllLinesAsync(trackJsonlPath, joined, cancellationToken);
    }

    private static IEnumerable<Candidate> EnumerateCandidates(string releaseLine)
    {
        if (string.IsNullOrWhiteSpace(releaseLine))
        {
            yield break;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(releaseLine);
        }
        catch (JsonException)
        {
            yield break;
        }

        using (document)
        {
            var root = document.RootElement;
            if (!TryReadReleaseGroup(root, out var releaseGroupId, out var releaseGroupTitle))
            {
                yield break;
            }

            var releaseDate = TryReadReleaseDate(root);
            var hasReleaseCredits = TryGetArtistCredit(root, out var releaseCredits);

            if (!root.TryGetProperty("media", out var media) || media.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var medium in media.EnumerateArray())
            {
                if (!medium.TryGetProperty("tracks", out var tracks) || tracks.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var track in tracks.EnumerateArray())
                {
                    if (!track.TryGetProperty("recording", out var recording) ||
                        recording.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (!recording.TryGetProperty("id", out var recordingIdProperty))
                    {
                        continue;
                    }

                    var recordingId = recordingIdProperty.GetString();
                    if (string.IsNullOrWhiteSpace(recordingId))
                    {
                        continue;
                    }

                    var title = FirstNonEmpty(
                        GetString(recording, "title"),
                        GetString(track, "title"));
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    JsonElement artistCredit;
                    if (TryGetArtistCredit(recording, out artistCredit))
                    {
                        // recording credits
                    }
                    else if (TryGetArtistCredit(track, out artistCredit))
                    {
                        // track credits
                    }
                    else if (hasReleaseCredits)
                    {
                        artistCredit = releaseCredits;
                    }
                    else
                    {
                        continue;
                    }

                    var length = TryGetLength(recording) ?? TryGetLength(track);
                    var jsonLine = BuildTrackLine(
                        recordingId,
                        title,
                        length,
                        artistCredit,
                        releaseGroupId,
                        releaseGroupTitle,
                        releaseDate);
                    yield return new Candidate(
                        $"{recordingId}\n{releaseGroupId}",
                        releaseDate,
                        jsonLine);
                }
            }
        }
    }

    private static bool IsPreferable(Candidate candidate, Candidate existing)
    {
        if (candidate.ReleaseDate is null && existing.ReleaseDate is null)
        {
            return false;
        }

        if (candidate.ReleaseDate is null)
        {
            return false;
        }

        if (existing.ReleaseDate is null)
        {
            return true;
        }

        return candidate.ReleaseDate < existing.ReleaseDate;
    }

    private static bool TryReadReleaseGroup(
        JsonElement release,
        out string releaseGroupId,
        out string releaseGroupTitle)
    {
        releaseGroupId = string.Empty;
        releaseGroupTitle = string.Empty;
        if (!release.TryGetProperty("release-group", out var releaseGroup) ||
            releaseGroup.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        releaseGroupId = GetString(releaseGroup, "id") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(releaseGroupId))
        {
            return false;
        }

        releaseGroupTitle = FirstNonEmpty(
            GetString(releaseGroup, "title"),
            GetString(releaseGroup, "name")) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(releaseGroupTitle);
    }

    private static DateOnly? TryReadReleaseDate(JsonElement release)
    {
        var raw = GetString(release, "date");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        if (DateOnly.TryParseExact(raw, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return date;
        }

        if (int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var year) &&
            year is >= 1 and <= 9999)
        {
            return new DateOnly(year, 1, 1);
        }

        return null;
    }

    private static bool TryGetArtistCredit(JsonElement element, out JsonElement credits)
    {
        if (element.TryGetProperty("artist-credit", out credits) &&
            credits.ValueKind == JsonValueKind.Array &&
            credits.GetArrayLength() > 0)
        {
            return true;
        }

        credits = default;
        return false;
    }

    private static int? TryGetLength(JsonElement element)
    {
        if (!element.TryGetProperty("length", out var lengthProperty) ||
            lengthProperty.ValueKind != JsonValueKind.Number ||
            !lengthProperty.TryGetInt32(out var length))
        {
            return null;
        }

        return length;
    }

    private static string BuildTrackLine(
        string recordingId,
        string title,
        int? length,
        JsonElement artistCredit,
        string releaseGroupId,
        string releaseGroupTitle,
        DateOnly? releaseDate)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("id", recordingId);
            writer.WriteString("title", title);
            if (length is not null)
            {
                writer.WriteNumber("length", length.Value);
            }

            writer.WritePropertyName("artist-credit");
            artistCredit.WriteTo(writer);

            writer.WritePropertyName("release-group");
            writer.WriteStartObject();
            writer.WriteString("id", releaseGroupId);
            writer.WriteString("title", releaseGroupTitle);
            writer.WriteEndObject();

            if (releaseDate is not null)
            {
                writer.WriteString(
                    "release-date",
                    releaseDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

    private sealed record Candidate(string Key, DateOnly? ReleaseDate, string JsonLine);
}

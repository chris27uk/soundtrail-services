using Soundtrail.Domain.Catalog;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Input;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

public sealed class FileSystemMusicBrainzDumpReader : IReadMusicBrainzDumpPort
{
    public async IAsyncEnumerable<MusicBrainzCatalogSeedRecord> ReadAsync(
        IReadOnlyList<string> recordingDumpPaths,
        IReadOnlyList<string> releaseDumpPaths,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var path in recordingDumpPaths)
        {
            await foreach (var record in ReadRecordingDumpAsync(path, cancellationToken))
            {
                yield return record;
            }
        }

        foreach (var path in releaseDumpPaths)
        {
            await foreach (var record in ReadReleaseDumpAsync(path, cancellationToken))
            {
                yield return record;
            }
        }
    }

    private static async IAsyncEnumerable<MusicBrainzCatalogSeedRecord> ReadRecordingDumpAsync(
        string path,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var line in ReadJsonLinesAsync(path, cancellationToken))
        {
            using var document = JsonDocument.Parse(line);
            var recordingId = GetString(document.RootElement, "id");
            var title = GetString(document.RootElement, "title");
            var durationMs = GetInt32(document.RootElement, "length");
            var isrc = GetFirstString(document.RootElement, "isrcs");
            var (artistName, artistId) = ReadArtist(document.RootElement);
            var release = GetFirstObject(document.RootElement, "releases");

            yield return new MusicBrainzCatalogSeedRecord(
                SourceRecordKey: $"recording:{recordingId}",
                SourceTrackId: recordingId ?? title ?? string.Empty,
                Title: title ?? string.Empty,
                Artist: artistName ?? string.Empty,
                SourceArtistId: artistId,
                AlbumTitle: release is null ? null : GetString(release.Value, "title"),
                SourceAlbumId: release is null ? null : GetString(release.Value, "id"),
                Isrc: isrc,
                MusicBrainzRecordingId: recordingId,
                DurationMs: durationMs,
                ReleaseDate: release is null ? null : ParseDate(GetString(release.Value, "date")));
        }
    }

    private static async IAsyncEnumerable<MusicBrainzCatalogSeedRecord> ReadReleaseDumpAsync(
        string path,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var line in ReadJsonLinesAsync(path, cancellationToken))
        {
            using var document = JsonDocument.Parse(line);
            var releaseId = GetString(document.RootElement, "id");
            var releaseTitle = GetString(document.RootElement, "title");
            var releaseDate = ParseDate(GetString(document.RootElement, "date"));
            var releaseArtist = ReadArtist(document.RootElement);

            if (!document.RootElement.TryGetProperty("media", out var mediaElement) || mediaElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var mediumIndex = 0;
            foreach (var media in mediaElement.EnumerateArray())
            {
                mediumIndex++;

                if (!media.TryGetProperty("tracks", out var tracksElement) || tracksElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var track in tracksElement.EnumerateArray())
                {
                    var trackId = GetString(track, "id");
                    var recording = GetObject(track, "recording");
                    var recordingId = recording is null ? null : GetString(recording.Value, "id");
                    var title = GetString(track, "title")
                        ?? (recording is null ? null : GetString(recording.Value, "title"));
                    var durationMs = GetInt32(track, "length")
                        ?? (recording is null ? null : GetInt32(recording.Value, "length"));
                    var isrc = GetFirstString(track, "isrcs")
                        ?? (recording is null ? null : GetFirstString(recording.Value, "isrcs"));
                    var artist = recording is null ? releaseArtist : ReadArtist(recording.Value, releaseArtist);

                    yield return new MusicBrainzCatalogSeedRecord(
                        SourceRecordKey: $"release:{releaseId}:medium:{mediumIndex}:track:{trackId ?? recordingId ?? title}",
                        SourceTrackId: recordingId ?? trackId ?? title ?? string.Empty,
                        Title: title ?? string.Empty,
                        Artist: artist.Name ?? string.Empty,
                        SourceArtistId: artist.Id,
                        AlbumTitle: releaseTitle,
                        SourceAlbumId: releaseId,
                        Isrc: isrc,
                        MusicBrainzRecordingId: recordingId,
                        DurationMs: durationMs,
                        ReleaseDate: releaseDate);
                }
            }
        }
    }

    private static async IAsyncEnumerable<string> ReadJsonLinesAsync(
        string path,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            yield return line;
        }
    }

    private static (string? Name, string? Id) ReadArtist(
        JsonElement element,
        (string? Name, string? Id)? fallback = null)
    {
        if (!element.TryGetProperty("artist-credit", out var artistCreditElement)
            || artistCreditElement.ValueKind != JsonValueKind.Array)
        {
            return fallback ?? default;
        }

        foreach (var artistCredit in artistCreditElement.EnumerateArray())
        {
            var artist = GetObject(artistCredit, "artist");
            var artistName = artist is null
                ? GetString(artistCredit, "name")
                : GetString(artist.Value, "name") ?? GetString(artistCredit, "name");
            var artistId = artist is null ? null : GetString(artist.Value, "id");

            if (!string.IsNullOrWhiteSpace(artistName))
            {
                return (artistName, artistId);
            }
        }

        return fallback ?? default;
    }

    private static JsonElement? GetObject(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : null;

    private static JsonElement? GetFirstObject(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                return item;
            }
        }

        return null;
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string? GetFirstString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                return item.GetString();
            }
        }

        return null;
    }

    private static int? GetInt32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, out var fullDate))
        {
            return fullDate;
        }

        var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1 && int.TryParse(parts[0], out var yearOnly))
        {
            return new DateOnly(yearOnly, 1, 1);
        }

        if (parts.Length == 2
            && int.TryParse(parts[0], out var year)
            && int.TryParse(parts[1], out var month))
        {
            return new DateOnly(year, month, 1);
        }

        return null;
    }
}

using System.Globalization;
using System.Text.Json;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;

public sealed class MusicBrainzTrackDumpRowMapper : IMusicBrainzTrackDumpRowMapper
{
    public Track? TryMap(string jsonLine)
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
                !root.TryGetProperty("track", out var trackElement))
            {
                return null;
            }

            var creditedArtistId = creditedArtistProperty.GetString();
            if (string.IsNullOrWhiteSpace(creditedArtistId))
            {
                return null;
            }

            if (!trackElement.TryGetProperty("id", out var recordingIdProperty))
            {
                return null;
            }

            var recordingId = recordingIdProperty.GetString();
            if (string.IsNullOrWhiteSpace(recordingId))
            {
                return null;
            }

            var title = trackElement.TryGetProperty("title", out var titleProperty)
                ? titleProperty.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            if (!TryReadReleaseGroup(trackElement, out var releaseGroupId, out var albumTitle) ||
                string.IsNullOrWhiteSpace(albumTitle))
            {
                return null;
            }

            var artistName = TryReadCreditedArtistName(trackElement, creditedArtistId);
            if (string.IsNullOrWhiteSpace(artistName))
            {
                return null;
            }

            var releaseDate = TryReadReleaseDate(trackElement);
            var trackIdResult = TrackId.TryCreate(artistName, title, albumTitle, releaseDate);
            if (trackIdResult is not TrackIdCreateResult.Success success)
            {
                return null;
            }

            int? durationMs = null;
            if (trackElement.TryGetProperty("length", out var lengthProperty) &&
                lengthProperty.ValueKind == JsonValueKind.Number &&
                lengthProperty.TryGetInt32(out var length))
            {
                durationMs = length;
            }

            var track = new Track(success.Value)
            {
                Title = title,
                ArtistName = artistName,
                AlbumTitle = albumTitle,
                AlbumId = AlbumId.From(creditedArtistId, releaseGroupId).StableValue,
                DurationMs = durationMs,
                ReleaseDate = releaseDate,
                UpdatedAt = default
            };
            SourceSystemIdSet.UnionWith(track.SourceSystemIds, SourceSystemIdSet.FromLegacyMusicBrainz(recordingId));
            return track;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryReadReleaseGroup(
        JsonElement trackElement,
        out string releaseGroupId,
        out string? albumTitle)
    {
        releaseGroupId = string.Empty;
        albumTitle = null;
        if (!trackElement.TryGetProperty("release-group", out var releaseGroup) ||
            releaseGroup.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!releaseGroup.TryGetProperty("id", out var idProperty))
        {
            return false;
        }

        releaseGroupId = idProperty.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(releaseGroupId))
        {
            return false;
        }

        albumTitle = releaseGroup.TryGetProperty("title", out var titleProperty)
            ? titleProperty.GetString()
            : releaseGroup.TryGetProperty("name", out var nameProperty)
                ? nameProperty.GetString()
                : null;
        return true;
    }

    private static string? TryReadCreditedArtistName(JsonElement trackElement, string creditedArtistId)
    {
        if (!trackElement.TryGetProperty("artist-credit", out var credits) ||
            credits.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var credit in credits.EnumerateArray())
        {
            if (!credit.TryGetProperty("artist", out var artist))
            {
                continue;
            }

            if (!artist.TryGetProperty("id", out var idProperty) ||
                !string.Equals(idProperty.GetString(), creditedArtistId, StringComparison.Ordinal))
            {
                continue;
            }

            return artist.TryGetProperty("name", out var nameProperty)
                ? nameProperty.GetString()
                : credit.TryGetProperty("name", out var creditNameProperty)
                    ? creditNameProperty.GetString()
                    : null;
        }

        return null;
    }

    private static DateOnly? TryReadReleaseDate(JsonElement trackElement)
    {
        if (!trackElement.TryGetProperty("release-date", out var dateProperty) ||
            dateProperty.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var raw = dateProperty.GetString();
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
}

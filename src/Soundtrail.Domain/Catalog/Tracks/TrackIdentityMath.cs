using System.Buffers.Binary;
using System.Text;
using Blake2Fast;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks.Parsing;

namespace Soundtrail.Domain.Catalog.Tracks;

public static class TrackIdentityMath
{
    private const int MaxArtistLength = 1000;
    private const int MaxTrackLength = 250;
    private const int MaxAlbumLength = 250;
    private const int MaxReleaseTypeLength = 50;

    public static TrackIdentityCanonicalizeResult TryCanonicalize(
        string artistName,
        string trackName,
        string? albumName,
        DateOnly? releaseDate,
        string? releaseType)
    {
        var parsedTitle = SongTitleParser.Parse(trackName);
        if (parsedTitle is SongTitleParseResult.Failure titleFailure)
        {
            return new TrackIdentityCanonicalizeResult.Failure(
                $"Track title could not be parsed: {titleFailure.Reason}.");
        }

        var title = ((SongTitleParseResult.Success)parsedTitle).Value;

        if (!TryCanonicalizeRequired(artistName, MaxArtistLength, out var canonicalArtist, out var artistReason))
        {
            return new TrackIdentityCanonicalizeResult.Failure(artistReason);
        }

        if (!TryCanonicalizeRequired(title.CanonicalTrackTitle.Value, MaxTrackLength, out var canonicalTrack, out var trackReason))
        {
            return new TrackIdentityCanonicalizeResult.Failure(trackReason);
        }

        if (!TryCanonicalizeOptional(albumName, MaxAlbumLength, out var canonicalAlbum, out var albumReason))
        {
            return new TrackIdentityCanonicalizeResult.Failure(albumReason);
        }

        if (!TryCanonicalizeOptional(
                string.IsNullOrWhiteSpace(releaseType)
                    ? title.CanonicalReleaseType?.Value
                    : releaseType,
                MaxReleaseTypeLength,
                out var canonicalReleaseType,
                out var releaseTypeReason))
        {
            return new TrackIdentityCanonicalizeResult.Failure(releaseTypeReason);
        }

        return new TrackIdentityCanonicalizeResult.Success(
            new CanonicalTrackIdentityParts(
                canonicalArtist!,
                canonicalTrack!,
                canonicalAlbum,
                releaseDate,
                canonicalReleaseType));
    }

    public static string CreateBaseComponent(CanonicalTrackIdentityParts parts) =>
        Convert.ToHexStringLower(Blake2b(Encode(parts.ArtistName, parts.TrackName), hashSizeBits: 128));

    public static TrackVector CreateVector(CanonicalTrackIdentityParts parts) =>
        new(
            CreateDiscriminator(parts.AlbumName),
            parts.ReleaseDate?.DayNumber,
            CreateDiscriminator(parts.ReleaseType));

    public static uint CreateDiscriminator(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var hash = Blake2b(Encoding.UTF8.GetBytes(value), hashSizeBits: 32);
        return BinaryPrimitives.ReadUInt32BigEndian(hash);
    }

    private static bool TryCanonicalizeRequired(
        string value,
        int maxLength,
        out string? canonical,
        out string reason)
    {
        canonical = MusicIdentityText.NormalizeFreeText(value);
        if (string.IsNullOrWhiteSpace(canonical))
        {
            reason = "Identity value is required.";
            return false;
        }

        if (canonical.Length > maxLength)
        {
            reason = $"Identity value exceeds max length {maxLength}.";
            canonical = null;
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryCanonicalizeOptional(
        string? value,
        int maxLength,
        out string? canonical,
        out string reason)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            canonical = null;
            reason = string.Empty;
            return true;
        }

        canonical = MusicIdentityText.NormalizeFreeText(value);
        if (string.IsNullOrWhiteSpace(canonical))
        {
            canonical = null;
            reason = string.Empty;
            return true;
        }

        if (canonical.Length > maxLength)
        {
            canonical = null;
            reason = $"Identity value exceeds max length {maxLength}.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static byte[] Encode(params string?[] parts)
    {
        var text = string.Join("|", parts.Select(static part => string.IsNullOrWhiteSpace(part) ? "~" : part));
        return Encoding.UTF8.GetBytes(text);
    }

    private static byte[] Blake2b(byte[] bytes, int hashSizeBits)
    {
        return global::Blake2Fast.Blake2b.ComputeHash(hashSizeBits / 8, bytes).ToArray();
    }
}

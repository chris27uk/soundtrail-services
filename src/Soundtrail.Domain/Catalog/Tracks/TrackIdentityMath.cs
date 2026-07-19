using System.Text;
using Blake2Fast;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Catalog.Tracks;

public static class TrackIdentityMath
{
    private const int MaxArtistLength = 1000;
    private const int MaxTrackLength = 250;
    private const int MaxAlbumLength = 250;
    private const int MaxReleaseTypeLength = 50;

    public static CanonicalTrackIdentityParts Canonicalize(
        string artistName,
        string trackName,
        string? albumName,
        DateOnly? releaseDate,
        string? releaseType)
    {
        var canonicalArtist = CanonicalizeRequired(artistName, MaxArtistLength, nameof(artistName));
        var canonicalTrack = CanonicalizeRequired(trackName, MaxTrackLength, nameof(trackName));
        var canonicalAlbum = CanonicalizeOptional(albumName, MaxAlbumLength, nameof(albumName));
        var canonicalReleaseType = CanonicalizeOptional(releaseType, MaxReleaseTypeLength, nameof(releaseType));

        return new CanonicalTrackIdentityParts(
            canonicalArtist,
            canonicalTrack,
            canonicalAlbum,
            releaseDate,
            canonicalReleaseType);
    }

    public static TrackIdKeyParts DeriveKeys(CanonicalTrackIdentityParts parts)
    {
        var baseHash = Blake2b(Encode(parts.ArtistName, parts.TrackName, parts.AlbumName), hashSizeBits: 256);
        var specificHash = Blake2b(Encode(parts.ReleaseDate?.ToString("yyyy-MM-dd"), parts.ReleaseType), hashSizeBits: 128);

        return new TrackIdKeyParts(
            ToHex(baseHash[..16]),
            ToHex(baseHash[16..32]),
            ToHex(specificHash));
    }

    private static string CanonicalizeRequired(string value, int maxLength, string paramName)
    {
        var canonical = MusicIdentityText.NormalizeFreeText(value);
        if (string.IsNullOrWhiteSpace(canonical))
        {
            throw new ArgumentException("Identity value is required.", paramName);
        }

        if (canonical.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Identity value exceeds max length {maxLength}.");
        }

        return canonical;
    }

    private static string? CanonicalizeOptional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var canonical = MusicIdentityText.NormalizeFreeText(value);
        if (string.IsNullOrWhiteSpace(canonical))
        {
            return null;
        }

        if (canonical.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Identity value exceeds max length {maxLength}.");
        }

        return canonical;
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

    private static string ToHex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(bytes).ToLowerInvariant();
}

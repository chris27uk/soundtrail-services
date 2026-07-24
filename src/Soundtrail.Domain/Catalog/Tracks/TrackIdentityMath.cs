using System.Buffers.Binary;
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

}

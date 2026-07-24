using System.Globalization;

namespace Soundtrail.Domain.Catalog.Tracks;

public readonly record struct TrackId
{
    private const string Prefix = "trk2_";
    private const int BaseComponentLength = 32;
    private const int VectorSegmentLength = 8;
    private const int PayloadLength = BaseComponentLength + (VectorSegmentLength * 3);

    private TrackId(
        string value,
        string baseComponent,
        TrackVector vector)
    {
        Value = value;
        BaseComponent = baseComponent;
        Vector = vector;
    }

    public string Value { get; }

    public string BaseComponent { get; }

    public TrackVector Vector { get; }

    public static TrackId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Track id is required.", nameof(value));
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!normalized.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("Track id must be a packed track id.", nameof(value));
        }

        var body = normalized[Prefix.Length..];
        if (body.Length != PayloadLength)
        {
            throw new ArgumentException("Track id must contain fixed-width base and vector segments.", nameof(value));
        }

        var baseComponent = NormalizeBaseComponent(body[..BaseComponentLength], nameof(value));
        var vector = new TrackVector(
            ParseUInt32Hex(body.AsSpan(BaseComponentLength, VectorSegmentLength), nameof(value)),
            ParseNullableDayNumber(body.AsSpan(BaseComponentLength + VectorSegmentLength, VectorSegmentLength), nameof(value)),
            ParseUInt32Hex(body.AsSpan(BaseComponentLength + (VectorSegmentLength * 2), VectorSegmentLength), nameof(value)));

        return new TrackId(normalized, baseComponent, vector);
    }

    public static TrackId Create(
        string artistName,
        string trackName,
        string? albumName = null,
        DateOnly? releaseDate = null,
        string? releaseType = null)
    {
        var canonical = TrackIdentityMath.Canonicalize(
            artistName,
            trackName,
            albumName,
            releaseDate,
            releaseType);

        return Create(canonical);
    }

    public static TrackId Create(CanonicalTrackIdentityParts parts)
    {
        var baseComponent = TrackIdentityMath.CreateBaseComponent(parts);
        var vector = TrackIdentityMath.CreateVector(parts);
        return new TrackId(
            BuildValue(baseComponent, vector),
            baseComponent,
            vector);
    }

    public static string ProjectBase(TrackId trackId) => trackId.BaseComponent;

    public static TrackVector ProjectVector(TrackId trackId) => trackId.Vector;

    public bool SharesBaseWith(TrackId other) =>
        string.Equals(BaseComponent, other.BaseComponent, StringComparison.Ordinal);

    public override string ToString() => Value;

    public static implicit operator string(TrackId trackId) => trackId.Value;

    private static string BuildValue(string baseComponent, TrackVector vector)
    {
        var albumSegment = vector.AlbumDiscriminator.ToString("x8", CultureInfo.InvariantCulture);
        var releaseDateSegment = (uint)(vector.ReleaseDateOrdinal ?? 0);
        var releaseTypeSegment = vector.ReleaseTypeDiscriminator.ToString("x8", CultureInfo.InvariantCulture);
        return string.Create(
            Prefix.Length + PayloadLength,
            (baseComponent, albumSegment, releaseDateSegment, releaseTypeSegment),
            static (span, state) =>
            {
                Prefix.AsSpan().CopyTo(span);
                var offset = Prefix.Length;
                state.baseComponent.AsSpan().CopyTo(span[offset..]);
                offset += BaseComponentLength;
                state.albumSegment.AsSpan().CopyTo(span[offset..]);
                offset += VectorSegmentLength;
                state.releaseDateSegment.TryFormat(span[offset..], out _, "x8", CultureInfo.InvariantCulture);
                offset += VectorSegmentLength;
                state.releaseTypeSegment.AsSpan().CopyTo(span[offset..]);
            });
    }

    private static string NormalizeBaseComponent(string value, string paramName)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length != BaseComponentLength)
        {
            throw new ArgumentException("Track id base component is required.", paramName);
        }

        foreach (var character in normalized)
        {
            if (!Uri.IsHexDigit(character))
            {
                throw new ArgumentException("Track id base component must be hexadecimal.", paramName);
            }
        }

        return normalized;
    }

    private static uint ParseUInt32Hex(ReadOnlySpan<char> value, string paramName)
    {
        if (!uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException("Track id vector segment is invalid.", paramName);
        }

        return result;
    }

    private static int? ParseNullableDayNumber(ReadOnlySpan<char> value, string paramName)
    {
        var result = ParseUInt32Hex(value, paramName);
        if (result == 0)
        {
            return null;
        }

        if (result > int.MaxValue)
        {
            throw new ArgumentException("Track id release date segment is out of range.", paramName);
        }

        return (int)result;
    }
}

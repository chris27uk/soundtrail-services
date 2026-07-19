namespace Soundtrail.Domain.Catalog.Tracks;

public readonly record struct TrackId
{
    private const string Prefix = "trk_";
    private const int HexPartLength = 32;
    private const int ExpectedValueLength = 100;

    private TrackId(
        string baseKeyHigh,
        string baseKeyLow,
        string specificKey)
    {
        BaseKeyHigh = NormalizeRequiredKey(baseKeyHigh, nameof(baseKeyHigh));
        BaseKeyLow = NormalizeRequiredKey(baseKeyLow, nameof(baseKeyLow));
        SpecificKey = NormalizeRequiredKey(specificKey, nameof(specificKey));
    }

    public string Value => $"{Prefix}{BaseKeyHigh}{BaseKeyLow}{SpecificKey}";

    public string BaseKeyHigh { get; }

    public string BaseKeyLow { get; }

    public string SpecificKey { get; }

    public static TrackId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Track id is required.", nameof(value));
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!normalized.StartsWith(Prefix, StringComparison.Ordinal) || normalized.Length != ExpectedValueLength)
        {
            throw new ArgumentException("Track id must be a structured track id.", nameof(value));
        }

        var body = normalized[Prefix.Length..];
        return new TrackId(
            body[..HexPartLength],
            body[HexPartLength..(HexPartLength * 2)],
            body[(HexPartLength * 2)..]);
    }

    public static TrackId FromKeyParts(
        string baseKeyHigh,
        string baseKeyLow,
        string specificKey) =>
        new(baseKeyHigh, baseKeyLow, specificKey);

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
        var keyParts = TrackIdentityMath.DeriveKeys(parts);
        return new(keyParts.BaseKeyHigh, keyParts.BaseKeyLow, keyParts.SpecificKey);
    }

    public TrackIdKeyParts GetKeyParts() =>
        new(
            BaseKeyHigh,
            BaseKeyLow,
            SpecificKey);

    public override string ToString() => Value;

    public static implicit operator string(TrackId trackId) => trackId.Value;

    private static string NormalizeRequiredKey(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Track id key is required.", paramName);
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length != HexPartLength || !normalized.All(IsLowerHex))
        {
            throw new ArgumentException("Track id key must be a 128-bit lowercase hexadecimal value.", paramName);
        }

        return normalized;
    }

    private static bool IsLowerHex(char value) =>
        (value >= '0' && value <= '9')
        || (value >= 'a' && value <= 'f');
}

using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog;

public readonly record struct TrackMatchFingerprint : IValueType
{
    private TrackMatchFingerprint(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Track match fingerprint is required.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public string StableValue => Value;

    public static TrackMatchFingerprint From(string value) => new(value);

    public static TrackMatchFingerprint FromArtistAndTitle(string artistName, string trackTitle) =>
        new($"{NormalizeComponent(artistName)}:{NormalizeComponent(trackTitle)}");

    public static TrackMatchFingerprint FromArtistAlbumAndTitle(string artistName, string albumTitle, string trackTitle) =>
        new($"{NormalizeComponent(artistName)}:{NormalizeComponent(albumTitle)}:{NormalizeComponent(trackTitle)}");

    public override string ToString() => Value;

    public static implicit operator string(TrackMatchFingerprint fingerprint) => fingerprint.Value;

    private static string NormalizeComponent(string? value)
    {
        var normalized = MusicIdentityText.NormalizeMatchText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Track match fingerprint component is required.", nameof(value));
        }

        return normalized;
    }
}

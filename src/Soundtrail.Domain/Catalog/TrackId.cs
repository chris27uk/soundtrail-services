namespace Soundtrail.Domain.Catalog;

public readonly record struct TrackId
{
    private TrackId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Track id is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public static TrackId From(string value) => new(value);

    public override string ToString() => Value;

    public static implicit operator string(TrackId trackId) => trackId.Value;
}

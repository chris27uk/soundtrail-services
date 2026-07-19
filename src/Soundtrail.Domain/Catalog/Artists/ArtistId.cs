using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog.Artists;

public readonly record struct ArtistId : IValueType
{
    private ArtistId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Artist id is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public string StableValue => Value;

    public static ArtistId From(string value) => new(value);

    public override string ToString() => Value;

    public static implicit operator string(ArtistId artistId) => artistId.Value;
}

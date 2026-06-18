namespace Soundtrail.Domain.Catalog;

public readonly record struct ArtistId
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

    public static ArtistId From(string value) => new(value);

    public override string ToString() => Value;

    public static implicit operator string(ArtistId artistId) => artistId.Value;
}

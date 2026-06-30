using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog;

public readonly record struct AlbumId : IValueType
{
    private AlbumId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Album id is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public string StableValue => Value;

    public static AlbumId From(string value) => new(value);

    public override string ToString() => Value;

    public static implicit operator string(AlbumId albumId) => albumId.Value;
}

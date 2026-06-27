namespace Soundtrail.Contracts.Common;

public readonly record struct MusicCatalogId(string Value) : IValueType
{
    public string StableValue => Value;

    public static MusicCatalogId From(string value) => new(value);

    public override string ToString() => Value;

    public static implicit operator MusicCatalogId(string value) => new(value);

    public static implicit operator string(MusicCatalogId musicCatalogId) => musicCatalogId.Value;
}

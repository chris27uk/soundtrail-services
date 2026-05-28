namespace MusicResolver.Domain.ValueTypes;

public sealed record ArtistName
{
    private ArtistName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ArtistName From(string value) => new(value.Trim());

    public override string ToString() => Value;
}

namespace MusicResolver.Domain.ValueTypes;

public sealed record Isrc
{
    private Isrc(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Isrc From(string value) => new(value.Trim().ToUpperInvariant());

    public override string ToString() => Value;
}

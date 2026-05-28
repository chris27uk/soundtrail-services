namespace MusicResolver.Domain.ValueTypes;

public sealed record Mbid
{
    private Mbid(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Mbid From(string value) => new(value.Trim());

    public override string ToString() => Value;
}

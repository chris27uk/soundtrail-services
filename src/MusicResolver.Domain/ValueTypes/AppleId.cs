namespace MusicResolver.Domain.ValueTypes;

public sealed record AppleId
{
    private AppleId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static AppleId From(string value) => new(value.Trim());

    public override string ToString() => Value;
}

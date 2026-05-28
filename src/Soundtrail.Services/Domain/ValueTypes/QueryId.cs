namespace Soundtrail.Services.Domain.ValueTypes;

public sealed record QueryId
{
    private QueryId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static QueryId New() => new($"q_{Guid.NewGuid():N}");

    public static QueryId From(string value) => new(value.Trim());

    public override string ToString() => Value;
}

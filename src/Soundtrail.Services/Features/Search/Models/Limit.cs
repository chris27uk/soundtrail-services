namespace Soundtrail.Services.Features.Search.Models;

public readonly record struct Limit
{
    public const int DefaultValue = 10;
    public const int Maximum = 25;

    private Limit(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static Limit From(int? value)
    {
        var resolved = value ?? DefaultValue;

        if (resolved < 1 || resolved > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Limit must be between 1 and {Maximum}.");
        }

        return new Limit(resolved);
    }

    public override string ToString() => Value.ToString();
}

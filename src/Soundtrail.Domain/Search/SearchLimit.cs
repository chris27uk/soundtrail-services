namespace Soundtrail.Domain.Search;

public readonly record struct SearchLimit
{
    public const int DefaultValue = 25;
    public const int Maximum = 100;

    private SearchLimit(int value) => Value = value;

    public int Value { get; }

    public static SearchLimit From(int? value)
    {
        var resolved = value ?? DefaultValue;

        if (resolved < 1 || resolved > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Limit must be between 1 and {Maximum}.");
        }

        return new SearchLimit(resolved);
    }
}

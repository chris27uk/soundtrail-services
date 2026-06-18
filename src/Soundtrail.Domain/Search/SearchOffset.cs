namespace Soundtrail.Domain.Search;

public readonly record struct SearchOffset
{
    private SearchOffset(int value) => Value = value;

    public int Value { get; }

    public static SearchOffset From(int? value)
    {
        var resolved = value ?? 0;

        if (resolved < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Offset must be zero or greater.");
        }

        return new SearchOffset(resolved);
    }
}

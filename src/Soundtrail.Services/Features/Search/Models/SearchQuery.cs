namespace Soundtrail.Services.Features.Search.Models;

public sealed record SearchQuery
{
    private SearchQuery(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static SearchQuery From(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Search query is required.", nameof(value));
        }

        return new SearchQuery(trimmed);
    }

    public override string ToString() => Value;
}

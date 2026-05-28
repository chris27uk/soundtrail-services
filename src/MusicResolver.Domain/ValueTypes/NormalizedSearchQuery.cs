namespace MusicResolver.Domain.ValueTypes;

public sealed record NormalizedSearchQuery
{
    private NormalizedSearchQuery(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static NormalizedSearchQuery From(SearchQuery query)
    {
        return FromText(query.Value);
    }

    public static NormalizedSearchQuery FromText(string value)
    {
        var sanitized = new string(
            value
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character)
                    ? char.ToLowerInvariant(character)
                    : ' ')
                .ToArray());

        var normalized = string.Join(
            ' ',
            sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return new NormalizedSearchQuery(normalized);
    }

    public override string ToString() => Value;
}

namespace Soundtrail.Domain.Search;

public sealed record SearchTypesFilter(IReadOnlyList<SearchResultType> Types)
{
    public static SearchTypesFilter All { get; } = new(
    [
        SearchResultType.Artist,
        SearchResultType.Album,
        SearchResultType.Track
    ]);

    public bool Includes(SearchResultType type) => Types.Contains(type);

    public string ToQueryKeyType() =>
        string.Join(',', Types
            .Distinct()
            .OrderBy(type => type)
            .Select(type => type.ToString().ToLowerInvariant()));

    public static SearchTypesFilter Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return All;
        }

        var types = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseType)
            .Distinct()
            .ToArray();

        if (types.Length == 0)
        {
            throw new ArgumentException("At least one search type must be provided.", nameof(value));
        }

        return new SearchTypesFilter(types);
    }

    private static SearchResultType ParseType(string type) =>
        type switch
        {
            "artist" => SearchResultType.Artist,
            "album" => SearchResultType.Album,
            "track" => SearchResultType.Track,
            _ => throw new ArgumentException($"Unknown search type '{type}'.", nameof(type))
        };
}

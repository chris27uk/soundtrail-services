namespace Soundtrail.Domain.Model;

public readonly record struct AlbumTitle
{
    private AlbumTitle(string value)
    {
        Value = value;
        Normalized = Normalize(value);
    }

    public string Value { get; }

    public string Normalized { get; }

    public bool HasValue => !string.IsNullOrWhiteSpace(Value);

    public static AlbumTitle Empty => new(string.Empty);

    public static AlbumTitle From(string? value) => new(value?.Trim() ?? string.Empty);

    public override string ToString() => Value;

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(
            value
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character)
                    ? char.ToLowerInvariant(character)
                    : ' ')
                .ToArray());

        return string.Join(
            ' ',
            sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

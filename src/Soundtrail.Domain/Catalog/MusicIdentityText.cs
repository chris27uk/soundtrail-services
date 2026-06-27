namespace Soundtrail.Domain.Catalog;

public static class MusicIdentityText
{
    public static string NormalizeFreeText(string? value)
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

    public static string NormalizeCompact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    public static bool LooksLikeIsrc(string compactValue) =>
        compactValue.Length == 12
        && compactValue.Take(2).All(char.IsLetter)
        && compactValue.Skip(2).Take(3).All(char.IsLetterOrDigit)
        && compactValue.Skip(5).All(char.IsDigit);

    public static bool LooksLikeMusicBrainzId(string compactValue) =>
        compactValue.Length == 32
        && compactValue.All(static character =>
            char.IsDigit(character)
            || (character >= 'a' && character <= 'f'));
}

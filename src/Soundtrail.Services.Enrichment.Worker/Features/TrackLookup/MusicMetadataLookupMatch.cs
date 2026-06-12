namespace Soundtrail.Services.Enrichment.Worker.Features.TrackLookup;

public static class MusicMetadataLookupMatch
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var buffer = new char[value.Length];
        var index = 0;

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[index++] = char.ToLowerInvariant(character);
            }
            else if (char.IsWhiteSpace(character) && index > 0 && buffer[index - 1] != ' ')
            {
                buffer[index++] = ' ';
            }
        }

        return new string(buffer, 0, index).Trim();
    }

    public static bool TitleAndArtistMatch(
        string? expectedTitle,
        string? expectedArtist,
        string? actualTitle,
        string? actualArtist) =>
        Normalize(expectedTitle) == Normalize(actualTitle)
        && Normalize(expectedArtist) == Normalize(actualArtist);
}

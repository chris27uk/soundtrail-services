namespace Soundtrail.Domain.Catalog.Tracks.Parsing;

internal static class ReleaseTypeVocabulary
{
    private static readonly HashSet<string> RecognisedTerms =
    [
        "mix",
        "edit",
        "remix",
        "version",
        "instrumental",
        "release",
        "live"
    ];

    public static bool IsRecognised(string? value)
    {
        var normalised = Normalize(value).Value;
        if (string.IsNullOrWhiteSpace(normalised))
        {
            return false;
        }

        return normalised
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(RecognisedTerms.Contains);
    }

    public static ReleaseType Normalize(string? value) => ReleaseType.From(value);
}

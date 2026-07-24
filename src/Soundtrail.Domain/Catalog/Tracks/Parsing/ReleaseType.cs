namespace Soundtrail.Domain.Catalog.Tracks.Parsing;

public sealed record ReleaseType
{
    private ReleaseType(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ReleaseType From(string? value) =>
        new(MusicIdentityText.NormalizeFreeText(value));
}

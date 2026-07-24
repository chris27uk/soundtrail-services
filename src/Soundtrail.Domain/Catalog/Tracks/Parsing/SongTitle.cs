namespace Soundtrail.Domain.Catalog.Tracks.Parsing;

public sealed record SongTitle
{
    private SongTitle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static SongTitle From(string? value) =>
        new(MusicIdentityText.NormalizeFreeText(value));
}

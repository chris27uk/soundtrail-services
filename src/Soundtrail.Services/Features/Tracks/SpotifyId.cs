namespace Soundtrail.Services.Features.Tracks;

public sealed record SpotifyId
{
    private SpotifyId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static SpotifyId From(string value) => new(value.Trim());

    public override string ToString() => Value;
}

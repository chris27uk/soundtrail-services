namespace Soundtrail.Services.Api.Features.SearchMusic.Tracks;

public sealed record Mbid
{
    private Mbid(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Mbid From(string value) => new(value.Trim());
}

namespace Soundtrail.Services.Api.Features.SearchMusic.Tracks;

public sealed record ArtistName
{
    private ArtistName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ArtistName From(string value) => new(value.Trim());
}

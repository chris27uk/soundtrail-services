namespace Soundtrail.Services.Features.Tracks;

public sealed record TrackTitle
{
    private TrackTitle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static TrackTitle From(string value) => new(value.Trim());
}

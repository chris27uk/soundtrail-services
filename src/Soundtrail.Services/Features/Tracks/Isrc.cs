namespace Soundtrail.Services.Features.Tracks;

public sealed record Isrc
{
    private Isrc(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Isrc From(string value) => new(value.Trim().ToUpperInvariant());
}

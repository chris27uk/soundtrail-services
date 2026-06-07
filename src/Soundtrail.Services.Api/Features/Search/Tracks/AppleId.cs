namespace Soundtrail.Services.Api.Features.Search.Tracks;

public sealed record AppleId
{
    private AppleId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static AppleId From(string value) => new(value.Trim());
}

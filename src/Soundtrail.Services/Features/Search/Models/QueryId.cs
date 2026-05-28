namespace Soundtrail.Services.Features.Search.Models;

public sealed record QueryId
{
    private QueryId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static QueryId New() => new($"q_{Guid.NewGuid():N}");
}

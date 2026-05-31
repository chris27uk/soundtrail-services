namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record MusicCatalogId
{
    private MusicCatalogId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MusicCatalogId New() => new($"mc_{Guid.NewGuid():N}");

    public static MusicCatalogId From(string value) => new(value);
}

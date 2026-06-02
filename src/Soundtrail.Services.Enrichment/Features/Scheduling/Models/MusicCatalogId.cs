namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record MusicCatalogId
{
    private MusicCatalogId(string value) => Value = value;

    public string Value { get; }

    public static MusicCatalogId From(string value) => new(value);
    
    public static implicit operator MusicCatalogId(string id) => new(id);
}

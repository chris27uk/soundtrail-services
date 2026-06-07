namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

public sealed record MusicCatalogId
{
    private MusicCatalogId(string value) => Value = value;

    public string Value { get; }

    public static MusicCatalogId From(string value) => new(value);
    
    public static implicit operator MusicCatalogId(string id) => new(id);
    
    public static implicit operator string(MusicCatalogId id) => id.Value;
}

namespace Soundtrail.Contracts.Worker;

public sealed record ResolveCanonicalMetadataCommandDto(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId)
{
    private const string ProviderName = "MusicBrainz";
    
    public string TargetProvider => ProviderName;
}

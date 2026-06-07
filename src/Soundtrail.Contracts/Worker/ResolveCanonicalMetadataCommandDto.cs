namespace Soundtrail.Contracts.Worker;

public sealed record ResolveCanonicalMetadataCommand(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId)
{
    private const string ProviderName = "MusicBrainz";
    
    public string TargetProvider => ProviderName;
}

namespace Soundtrail.Contracts.Worker;

public sealed record ResolveApplePlaybackReferenceCommand(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId)
{
    private const string ProviderName = "AppleMusic";
    
    public string TargetProvider => ProviderName;
}

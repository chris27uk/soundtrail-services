namespace Soundtrail.Contracts.Worker;

public sealed record ResolveYouTubeMusicPlaybackReferenceCommand(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId)
{
    private const string ProviderName = "YouTubeMusic";
    
    public string TargetProvider => ProviderName;
}

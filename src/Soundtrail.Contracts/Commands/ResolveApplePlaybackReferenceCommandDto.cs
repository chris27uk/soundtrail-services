using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.Commands;

public sealed record ResolveApplePlaybackReferenceCommandDto(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId)
{
    private const string ProviderName = "AppleMusic";
    
    public string TargetProvider => ProviderName;
}

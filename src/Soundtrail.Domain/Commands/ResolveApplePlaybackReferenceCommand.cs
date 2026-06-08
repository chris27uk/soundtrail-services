using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Commands;

public sealed record ResolveApplePlaybackReferenceCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId)
{
    public ProviderName TargetProvider => ProviderName.AppleMusic;
}

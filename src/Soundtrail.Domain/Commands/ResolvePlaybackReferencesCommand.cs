using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record ResolvePlaybackReferencesCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    PlaybackReferenceLookupKey LookupKey)
{
    public ProviderName TargetProvider => ProviderName.Odesli;
}

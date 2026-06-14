using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record ResolvePlaybackReferencesCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicSearchTerm LookupKey) : LookupPhaseCommand(CommandId, MusicCatalogId, Priority, CreatedAt, CorrelationId)
{
    public ProviderName TargetProvider => ProviderName.Odesli;
}

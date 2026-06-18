using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record ResolvePlaybackReferencesCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicSearchTerm LookupKey,
    CatalogTrackHierarchy? Hierarchy = null) : IMusicCatalogLookupCommand
{
    public ProviderName TargetProvider => ProviderName.Odesli;
}

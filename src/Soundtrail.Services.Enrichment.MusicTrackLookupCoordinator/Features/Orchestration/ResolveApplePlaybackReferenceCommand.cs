using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Features.Orchestration;

public sealed record ResolveApplePlaybackReferenceCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId);

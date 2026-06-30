using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumDiscoveryCompleted.Support;

public sealed record KnownAlbumDiscoveryCompletedCommand(
    string DiscoveryStreamId,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistDiscoveryCompleted.Support;

public sealed record KnownArtistDiscoveryCompletedCommand(
    string DiscoveryStreamId,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

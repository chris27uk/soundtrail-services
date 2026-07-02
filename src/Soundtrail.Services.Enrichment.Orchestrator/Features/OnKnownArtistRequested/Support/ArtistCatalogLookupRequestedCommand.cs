using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Support;

public sealed record ArtistCatalogLookupRequestedCommand(
    string DiscoveryStreamId,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

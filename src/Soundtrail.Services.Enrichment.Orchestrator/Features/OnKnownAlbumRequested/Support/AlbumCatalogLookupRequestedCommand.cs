using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Support;

public sealed record AlbumCatalogLookupRequestedCommand(
    string DiscoveryStreamId,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

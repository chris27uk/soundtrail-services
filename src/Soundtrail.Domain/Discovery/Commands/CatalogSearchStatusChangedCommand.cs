using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Commands;

public sealed record CatalogSearchStatusChangedCommand(
    CatalogSearchCriteria Criteria,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

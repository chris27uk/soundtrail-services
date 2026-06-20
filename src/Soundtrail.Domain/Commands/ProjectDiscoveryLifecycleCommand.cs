using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Commands;

public sealed record ProjectDiscoveryLifecycleCommand(
    CatalogSearchCriteria Criteria,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

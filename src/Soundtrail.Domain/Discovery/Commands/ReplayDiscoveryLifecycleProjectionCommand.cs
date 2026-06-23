using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Commands;

public sealed record ReplayDiscoveryLifecycleProjectionCommand(CatalogSearchCriteria Criteria);

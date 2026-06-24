using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Commands;

public sealed record ReplayCatalogSearchStatusCommand(CatalogSearchCriteria Criteria);

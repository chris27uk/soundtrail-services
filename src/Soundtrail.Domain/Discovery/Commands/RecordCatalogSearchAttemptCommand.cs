using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Commands;

public sealed record RecordCatalogSearchAttemptCommand(
    CatalogSearchCriteria Criteria,
    CatalogSearchAttempt Request);

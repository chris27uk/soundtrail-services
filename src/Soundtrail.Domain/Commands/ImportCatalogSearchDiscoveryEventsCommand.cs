using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Commands;

public sealed record ImportCatalogSearchDiscoveryEventsCommand(
    CatalogSearchCriteria Criteria,
    int ExpectedVersion,
    IReadOnlyList<IDomainEvent> Events);

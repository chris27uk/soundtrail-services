using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;

public sealed record CatalogSearchPlannedForLookupCommand(
    CatalogSearchCriteria Criteria,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

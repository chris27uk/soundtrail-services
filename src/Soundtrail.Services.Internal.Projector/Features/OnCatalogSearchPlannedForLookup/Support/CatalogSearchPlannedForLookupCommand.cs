using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;

public sealed record CatalogSearchPlannedForLookupCommand(
    LookupCriteria SearchCriteria,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

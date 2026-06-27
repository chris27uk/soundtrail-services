using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record CatalogSearchStatusChangedCommand(
    MusicSearchCriteria SearchCriteria,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

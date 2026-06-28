using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Internal.Projector.Features.OnDiscoveryRequested.Support;

public sealed record DiscoveryRequestedCommand(
    DiscoveryQueryKey StreamId,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

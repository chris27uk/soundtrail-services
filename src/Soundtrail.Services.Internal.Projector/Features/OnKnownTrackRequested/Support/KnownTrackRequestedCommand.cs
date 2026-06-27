using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Support;

public sealed record KnownTrackRequestedCommand(
    KnownCatalogItem KnownItem,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Support;

public sealed record KnownTrackRequestedCommand(
    KnownCatalogId KnownId,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

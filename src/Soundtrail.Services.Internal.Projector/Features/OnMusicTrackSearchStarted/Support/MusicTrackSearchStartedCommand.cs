using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

public sealed record MusicTrackSearchStartedCommand(
    CatalogSearchCriteria Criteria,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

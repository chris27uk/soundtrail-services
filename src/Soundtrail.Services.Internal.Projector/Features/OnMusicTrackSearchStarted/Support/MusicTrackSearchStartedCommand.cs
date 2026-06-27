using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

public sealed record MusicTrackSearchStartedCommand(
    MusicSearchCriteria SearchCriteria,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);

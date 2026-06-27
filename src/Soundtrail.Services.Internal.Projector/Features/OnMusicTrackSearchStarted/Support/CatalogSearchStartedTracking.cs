using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

public sealed record CatalogSearchStartedTracking(MusicSearchCriteria SearchCriteria, string MusicCatalogId, DateTimeOffset UpdatedAt);

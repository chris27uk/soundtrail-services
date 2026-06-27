namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

public sealed record CatalogSearchStartedTracking(string Criteria, string MusicCatalogId, DateTimeOffset UpdatedAt);

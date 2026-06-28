using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Support;

public sealed record CatalogSearchCandidateTracking(MusicSearchCriteria SearchCriteria, string MusicCatalogId, DateTimeOffset UpdatedAt);

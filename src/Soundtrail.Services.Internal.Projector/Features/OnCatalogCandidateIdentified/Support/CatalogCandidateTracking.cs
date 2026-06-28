using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

public sealed record CatalogCandidateTracking(MusicSearchCriteria SearchCriteria, string MusicCatalogId, DateTimeOffset UpdatedAt);

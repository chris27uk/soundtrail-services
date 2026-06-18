using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogSearchTracking(CatalogSearchCriteria Criteria, MusicCatalogId MusicCatalogId, DateTimeOffset UpdatedAt);

using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogSearchTracking(MusicSearchCriteria SearchCriteria, MusicCatalogId MusicCatalogId, DateTimeOffset UpdatedAt);

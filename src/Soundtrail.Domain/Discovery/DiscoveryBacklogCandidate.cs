using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed record DiscoveryBacklogCandidate(
    MusicSearchCriteria SearchCriteria,
    MusicCatalogId MusicCatalogId,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? EarliestExpectedCompletionAt);

using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Aggregates;

namespace Soundtrail.Domain.Discovery;

public sealed record LookupResultContext(
    CatalogWorkId StreamId,
    CommandId OriginalCommandId);

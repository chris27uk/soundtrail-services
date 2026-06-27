using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record KnownAlbumRequested(
    AlbumId AlbumId,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);

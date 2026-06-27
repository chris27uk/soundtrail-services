using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record KnownArtistRequested(
    ArtistId ArtistId,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);

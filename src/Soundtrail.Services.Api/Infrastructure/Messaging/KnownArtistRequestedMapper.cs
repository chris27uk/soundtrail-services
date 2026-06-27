using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public static class KnownArtistRequestedMapper
{
    public static KnownArtistRequestedDto ToDto(KnownArtistRequested request) =>
        new(
            request.ArtistId.Value,
            request.OccurredAt,
            request.CorrelationId.Value);
}

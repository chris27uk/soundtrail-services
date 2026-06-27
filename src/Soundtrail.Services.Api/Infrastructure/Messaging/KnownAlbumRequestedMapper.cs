using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public static class KnownAlbumRequestedMapper
{
    public static KnownAlbumRequestedDto ToDto(KnownAlbumRequested request) =>
        new(
            request.AlbumId.Value,
            request.OccurredAt,
            request.CorrelationId.Value);
}

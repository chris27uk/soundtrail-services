using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public static class KnownTrackRequestedMapper
{
    public static KnownTrackRequestedDto ToDto(KnownTrackRequested request) =>
        new(
            request.TrackId.Value,
            request.Playback.ToString(),
            request.OccurredAt,
            request.CorrelationId.Value);
}

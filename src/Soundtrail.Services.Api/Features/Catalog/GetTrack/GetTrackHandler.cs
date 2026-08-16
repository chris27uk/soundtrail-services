using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack;

public sealed class GetTrackHandler(
    IGetTrackPort getTrackPort,
    ICommandBus commandBus,
    IClockPort clock) : IApiHandler<GetTrackRequest, GetTrackResponse?>
{
    public async Task<GetTrackResponse?> Handle(GetTrackRequest request, CancellationToken cancellationToken = default)
    {
        var response = await getTrackPort.GetTrackAsync(request.TrackId, cancellationToken);
        if (response is not null && response.StreamingLocations.Length == 0)
        {
            var requestedAt = clock.UtcNow;
            await commandBus.SendAsync(
                new RequestKnownMusicDataMessage(
                    new CatalogItemOperation.StreamingLocationForTrack(request.TrackId),
                    LookupPriorityBand.High,
                    TrustLevel: 100,
                    RiskScore: 0,
                    requestedAt)
                {
                    Id = MessageId.Deterministic(
                        "RequestKnownMusicData",
                        "get-track",
                        "streaming",
                        request.TrackId.Value),
                    CorrelationId = CorrelationId.From($"get-track:{request.TrackId.Value}")
                },
                cancellationToken);
        }

        return response;
    }
}

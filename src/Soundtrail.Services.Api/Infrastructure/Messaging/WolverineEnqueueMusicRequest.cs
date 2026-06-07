using Soundtrail.Contracts;
using Soundtrail.Contracts.Api;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Wolverine;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public sealed class WolverineEnqueueMusicRequest(
    IMessageBus messageBus) : IEnqueueMusicRequest
{
    public Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken) =>
        messageBus.SendAsync(
            new LookupMusicRequestDto(
                request.Query,
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                request.CorrelationId)).AsTask();
}

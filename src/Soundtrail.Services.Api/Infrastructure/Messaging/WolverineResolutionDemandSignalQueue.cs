using Soundtrail.Services.Features.Search.Contracts;
using Wolverine;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public sealed class WolverineResolutionDemandSignalQueue(
    IMessageBus messageBus) : IResolutionDemandSignalPort
{
    public Task EnqueueAsync(
        ResolutionDemandSignal signal,
        CancellationToken cancellationToken) =>
        messageBus.SendAsync(signal).AsTask();

    public ValueTask<ResolutionDemandSignal?> DequeueAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException("WolverineResolutionDemandSignalQueue only supports enqueue operations.");
}

using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Queueing;
using Wolverine;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public sealed class WolverineEnqueueMusicRequest(
    IMessageBus messageBus) : IEnqueueMusicRequest
{
    public Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken) =>
        messageBus.SendAsync(request).AsTask();
}

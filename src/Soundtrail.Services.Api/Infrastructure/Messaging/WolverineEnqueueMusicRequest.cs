using Soundtrail.Domain.Commands;
using Soundtrail.Services.Api.Features.SearchMusic.Queueing;
using Wolverine;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public sealed class WolverineEnqueueMusicRequest(
    IMessageBus messageBus) : IEnqueueMusicRequest
{
    public Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken) =>
        messageBus.SendAsync(LookupMusicRequestMapper.ToDto(request)).AsTask();
}
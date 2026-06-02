using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Wolverine;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class WolverineLookupMusicCommandQueue(
    IMessageBus messageBus) : ILookupMusicCommandQueue
{
    public Task EnqueueAsync(
        LookupMusicCommand command,
        CancellationToken cancellationToken)
    {
        object message = command.Priority == LookupPriorityBand.High
            ? new HighPriorityLookupMusicCommandMessage(command)
            : new LowPriorityLookupMusicCommandMessage(command);

        return messageBus.SendAsync(message).AsTask();
    }
}

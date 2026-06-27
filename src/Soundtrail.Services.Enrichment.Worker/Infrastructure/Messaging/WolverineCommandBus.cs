using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Responses;
using Wolverine;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class WolverineCommandBus(IMessageBus messageBus) : ICommandBus
{
    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        object message = command switch
        {
            MusicCatalogLookupAttempted attempted => attempted.ToDto(),
            _ => command
        };

        return messageBus.SendAsync(message).AsTask();
    }
}

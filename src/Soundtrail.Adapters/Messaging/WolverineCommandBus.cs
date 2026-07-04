using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Adapters.Registry;
using Wolverine;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Adapters.Messaging;

public sealed class WolverineCommandBus(
    IMessageBus messageBus) : ICommandBus
{
    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var registry = TypeTranslationRegistry.Default;
        object message;

        try
        {
            message = registry.ToDto(command);
        }
        catch (InvalidOperationException)
        {
            message = command;
        }

        return messageBus.SendAsync(message).AsTask();
    }
}

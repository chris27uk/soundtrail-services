using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Wolverine;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Adapters.Messaging;

internal sealed class WolverineCommandBus(IMessageBus messageBus) : ICommandBus
{
    public Task SendAsync(Soundtrail.Domain.Abstractions.IMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var registry = TypeTranslationRegistry.Default;
        object dtoOrCommand;
        try
        {
            dtoOrCommand = registry.ToDto(message);
        }
        catch (InvalidOperationException)
        {
            dtoOrCommand = message;
        }

        return messageBus.SendAsync(dtoOrCommand).AsTask();
    }

    public Task SendAsync(object message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message is Soundtrail.Domain.Abstractions.IMessage command)
        {
            return SendAsync(command, cancellationToken);
        }

        return messageBus.SendAsync(message).AsTask();
    }
}

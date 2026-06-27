using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

public sealed class WolverineCommandBus(Wolverine.IMessageBus messageBus) : Soundtrail.Domain.ICommandBus
{
    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default) =>
        messageBus.SendAsync(command.ToMessage()).AsTask();
}

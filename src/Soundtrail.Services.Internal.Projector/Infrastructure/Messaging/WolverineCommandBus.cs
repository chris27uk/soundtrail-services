using Soundtrail.Domain.Abstractions;
using Wolverine;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

public sealed class WolverineCommandBus(IMessageBus messageBus) : ICommandBus
{
    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default) =>
        messageBus.SendAsync(command.ToMessage()).AsTask();
}

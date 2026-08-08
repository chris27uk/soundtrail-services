using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

/// <summary>
/// Sociable message pump only — app dependencies live in DI via feature Ports/Configure.
/// </summary>
internal sealed class SociableMessagePump(CommandBusFake commandBus, HandlerCollection messageHandlers)
{
    public TMessage SentMessage<TMessage>() where TMessage : IMessage =>
        commandBus.SentMessages.OfType<TMessage>().Single();

    public IReadOnlyList<TMessage> SentMessages<TMessage>() where TMessage : IMessage =>
        commandBus.SentMessages.OfType<TMessage>().ToArray();

    public async Task<TResult> ProjectOnChange<TSut, TResult>(
        Func<TSut, Task<TResult>> change,
        TSut sut)
    {
        var result = await change(sut);
        await PumpAsync();
        return result;
    }

    public async Task PumpNextMessageAsync()
    {
        if (commandBus.TryDequeue(out var message))
        {
            await messageHandlers.HandleAsync(message, CancellationToken.None);
        }
    }

    public async Task PumpAsync()
    {
        for (var iteration = 0; iteration < 500 && commandBus.TryDequeue(out var message); iteration++)
        {
            await messageHandlers.HandleAsync(message, CancellationToken.None);
        }

        if (commandBus.Messages.Count > 0)
        {
            throw new InvalidOperationException("The sociable message pump did not drain all known work.");
        }
    }
}

internal sealed class DiscoveryEventProjector(IServiceScopeFactory scopeFactory)
{
    public async Task ProjectAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetRequiredService<HandlerCollection>();
        foreach (var @event in events)
        {
            await handlers.HandleAsync(@event, cancellationToken);
        }
    }
}

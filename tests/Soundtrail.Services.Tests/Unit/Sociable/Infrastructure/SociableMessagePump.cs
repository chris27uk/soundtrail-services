using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
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
        var deferredNotReady = new Queue<IMessage>();
        TrackLookupNotReadyException? lastNotReady = null;
        var madeProgress = false;

        for (var iteration = 0; iteration < 500; iteration++)
        {
            if (!commandBus.TryDequeue(out var message))
            {
                if (deferredNotReady.Count == 0)
                {
                    return;
                }

                if (!madeProgress)
                {
                    throw (Exception?)lastNotReady
                          ?? new InvalidOperationException(
                              "The sociable message pump stalled on track lookup that was not ready.");
                }

                // Replay deferred lookups after other work has had a chance to project tracks.
                madeProgress = false;
                while (deferredNotReady.Count > 0)
                {
                    commandBus.Requeue(deferredNotReady.Dequeue());
                }

                continue;
            }

            try
            {
                await messageHandlers.HandleAsync(message, CancellationToken.None);
                madeProgress = true;
            }
            catch (TrackLookupNotReadyException ex)
            {
                lastNotReady = ex;
                deferredNotReady.Enqueue(message);
            }
        }

        if (deferredNotReady.Count > 0 || commandBus.Messages.Count > 0)
        {
            throw (Exception?)lastNotReady
                  ?? new InvalidOperationException("The sociable message pump did not drain all known work.");
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

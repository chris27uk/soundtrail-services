using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

namespace Soundtrail.Services.Enrichment.Worker.Shared.Execution;

public sealed class IdempotentLookupHandlerDecorator<TMessage>(
    IHandler<TMessage> inner,
    ILookupDecoratorMetadata<TMessage> metadata,
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    ICommandBus commandBus,
    IClockPort clock) : IHandler<TMessage>
    where TMessage : IMessage
{
    public async Task Handle(TMessage request, CancellationToken cancellationToken = default)
    {
        var observedAt = clock.UtcNow;

        await using var session = await IdempotencySession.StartAsync(
            lookupExecutionReceiptStore,
            request.Id,
            cancellationToken);

        if (session.ProcessedBefore)
        {
            await commandBus.SendAsync(
                new CatalogLookupCompleted(
                    MessageId.New(),
                    request.RequestedAt,
                    request.CorrelationId,
                    new LookupResult.Duplicate(
                        metadata.CreateContext(request),
                        metadata.CreateExistingItem(request, observedAt),
                        "Lookup already completed.",
                        observedAt)),
                cancellationToken);
            return;
        }

        try
        {
            await inner.Handle(request, cancellationToken);
            await session.CompleteAsync(cancellationToken);
        }
        catch (LookupExecutionShortCircuitException)
        {
            await session.ReleaseAsync(cancellationToken);
        }
        catch
        {
            await session.ReleaseAsync(cancellationToken);
            throw;
        }
    }
}

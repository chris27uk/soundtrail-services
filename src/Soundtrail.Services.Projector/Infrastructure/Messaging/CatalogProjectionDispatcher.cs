using System.Diagnostics;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Projection;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class CatalogProjectionDispatcher(
    StoredEventDomainEventResolver resolver,
    HandlerCollection handlers)
{
    public const string SubscriptionName = "projector/catalog";

    public async Task DispatchAsync(RavenStoredEventRecord storedEvent, CancellationToken cancellationToken)
    {
        if (storedEvent.AggregateType is not "catalog-stream")
        {
            throw new InvalidOperationException(
                $"Unsupported catalog aggregate type '{storedEvent.AggregateType}'.");
        }

        var domainEvent = resolver.Resolve(storedEvent);
        using var activity = MessageTelemetry.StartHandleActivity(
            dtoTypeName: storedEvent.BodyType,
            domainEventName: MessageTelemetry.DomainEventNameFor(domainEvent.GetType()),
            correlationId: storedEvent.CorrelationId,
            sourceName: SubscriptionName,
            messageId: storedEvent.EventId);

        try
        {
            await handlers.HandleAsync(domainEvent, cancellationToken);
            MessageTelemetry.AddCurrentEvent("message.processed");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Activity.Current?.AddEvent(
                new ActivityEvent(
                    "exception",
                    tags: new ActivityTagsCollection
                    {
                        { "exception.type", ex.GetType().FullName },
                        { "exception.message", ex.Message }
                    }));
            throw;
        }
    }
}

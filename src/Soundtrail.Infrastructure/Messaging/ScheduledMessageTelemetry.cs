using System.Diagnostics;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Messaging;

public static class ScheduledMessageTelemetry
{
    public static async Task HandleAsync<TMessage>(
        IHandler<TMessage> handler,
        TMessage message,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        var correlationId = message is IMessage domainMessage
            ? domainMessage.CorrelationId.Value
            : null;

        using var activity = MessageTelemetry.StartHandleActivity(
            dtoTypeName: null,
            domainEventName: MessageTelemetry.DomainEventNameFor(typeof(TMessage)),
            correlationId: correlationId,
            sourceName: sourceName);

        try
        {
            await handler.Handle(IncomingMessage<TMessage>.Create(message), cancellationToken);
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

using System.Diagnostics;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Messaging;

public static class ScheduledMessageTelemetry
{
    public static async Task ExecuteAsync<TCommand>(
        TCommand command,
        string sourceName,
        Func<TCommand, CancellationToken, Task> execute,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentNullException.ThrowIfNull(execute);

        var correlationId = command is IMessage domainMessage
            ? domainMessage.CorrelationId.Value
            : null;

        using var activity = MessageTelemetry.StartHandleActivity(
            dtoTypeName: null,
            domainEventName: MessageTelemetry.DomainEventNameFor(typeof(TCommand)),
            correlationId: correlationId,
            sourceName: sourceName);

        try
        {
            await execute(command, cancellationToken);
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

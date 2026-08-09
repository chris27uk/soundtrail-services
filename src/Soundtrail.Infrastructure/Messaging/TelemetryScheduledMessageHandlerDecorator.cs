using System.Diagnostics;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Messaging;

public sealed class TelemetryScheduledMessageHandlerDecorator<TMessage>(
    IScheduledMessageHandler<TMessage> inner) : IScheduledMessageHandler<TMessage>
    where TMessage : IScheduledMessage
{
    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        using var activity = MessageTelemetry.StartScheduleActivity(message);

        try
        {
            await inner.HandleAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception"));
            throw;
        }
    }
}

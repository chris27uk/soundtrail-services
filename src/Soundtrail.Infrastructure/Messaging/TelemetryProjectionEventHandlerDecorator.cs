using System.Diagnostics;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Projection;

namespace Soundtrail.Adapters.Messaging;

public sealed class TelemetryProjectionEventHandlerDecorator<TEvent>(
    IProjectionEventHandler<TEvent> inner) : IProjectionEventHandler<TEvent>
{
    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        var stage = MessageTelemetry.StageNameFor(typeof(TEvent));
        using var activity = MessageTelemetry.StartHandlerActivity(typeof(TEvent), stage);

        if (activity is null)
        {
            MessageTelemetry.AddCurrentEvent($"{stage}.started");
        }

        try
        {
            await inner.HandleAsync(@event, cancellationToken);
            MessageTelemetry.AddCurrentEvent($"{stage}.completed");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            MessageTelemetry.AddCurrentEvent($"{stage}.failed");
            throw;
        }
    }
}

using System.Diagnostics;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Messaging;

public sealed class TelemetryHandlerDecorator<TMessage>(IHandler<TMessage> inner) : IHandler<TMessage>
{
    public async Task Handle(IncomingMessage<TMessage> context, CancellationToken cancellationToken = default)
    {
        var stage = MessageTelemetry.StageNameFor(typeof(TMessage));
        using var activity = context.Message is IMessage domainMessage
            ? MessageTelemetry.StartHandlerActivity(domainMessage, stage)
            : MessageTelemetry.StartHandlerActivity(typeof(TMessage), stage);

        if (activity is null)
        {
            MessageTelemetry.AddCurrentEvent($"{stage}.started");
        }

        try
        {
            await inner.Handle(context, cancellationToken);
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

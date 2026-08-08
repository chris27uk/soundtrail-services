using Soundtrail.Contracts.Common;
using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled;

public sealed class WorkScheduledProjectorHandler(
    ICommandBus commandBus) : IProjectionEventHandler<WorkScheduled>
{
    Task IProjectionEventHandler<WorkScheduled>.HandleAsync(
        WorkScheduled @event,
        CancellationToken cancellationToken) =>
        Handle(@event, cancellationToken);

    public async Task Handle(WorkScheduled @event, CancellationToken cancellationToken = default)
    {
        var command = new DispatchLookupWork(
            @event.Target,
            @event.Priority,
            MessageId.Deterministic(
                "DispatchLookupWork",
                @event.Target.NormalisedIdentifier,
                @event.ScheduledAt.ToString("O")),
            CorrelationId.From($"work-scheduled:{@event.Target.NormalisedIdentifier}:{@event.ScheduledAt:O}"),
            @event.ScheduledAt);

        await commandBus.SendAsync(command, cancellationToken);
    }
}

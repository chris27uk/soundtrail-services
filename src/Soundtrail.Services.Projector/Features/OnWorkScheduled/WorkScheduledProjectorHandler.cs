using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled;

public sealed class WorkScheduledProjectorHandler(
    ICommandBus commandBus,
    IStoreDiscoveryFeedbackPort storeDiscoveryFeedbackPort)
{
    public async Task Handle(WorkScheduled @event, CancellationToken cancellationToken = default)
    {
        var command = new DispatchLookupWork(
            @event.Target,
            @event.Priority,
            CommandId.For($"DispatchLookupWork:{@event.Target.NormalisedIdentifier}:{@event.ScheduledAt:O}"),
            CorrelationId.From($"work-scheduled:{@event.Target.NormalisedIdentifier}:{@event.ScheduledAt:O}"),
            @event.ScheduledAt);

        await commandBus.SendAsync(command, cancellationToken);
        await storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);
    }
}

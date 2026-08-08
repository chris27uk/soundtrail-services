using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkScheduled
{
    public class WorkScheduledEventHandler(IStoreDiscoveryFeedbackPort storeDiscoveryFeedbackPort) : IProjectionEventHandler<WorkScheduled>
    {
        public async Task HandleAsync(WorkScheduled @event, CancellationToken cancellationToken = default)
        {
            await storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);
        }
    }
}

using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkCompleted
{
    public class WorkCompletedEventHandler(IStoreDiscoveryFeedbackPort storeDiscoveryFeedbackPort) : IProjectionEventHandler<WorkCompleted>
    {
        public async Task HandleAsync(WorkCompleted @event, CancellationToken cancellationToken = default)
        {
            await storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);
        }
    }
}

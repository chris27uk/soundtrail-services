using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkDeferred
{
    public class WorkDeferredEventHandler(IStoreDiscoveryFeedbackPort storeDiscoveryFeedbackPort) : IProjectionEventHandler<WorkDeferred>
    {
        public async Task HandleAsync(WorkDeferred @event, CancellationToken cancellationToken = default)
        {
            await storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);
        }
    }
}

using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkRejected
{
    public class WorkRejectedEventHandler(IStoreDiscoveryFeedbackPort storeDiscoveryFeedbackPort) : IProjectionEventHandler<WorkRejected>
    {
        public async Task HandleAsync(WorkRejected @event, CancellationToken cancellationToken = default)
        {
            await storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);
        }
    }
}

using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkIgnored
{
    public class WorkIgnoredEventHandler(IStoreDiscoveryFeedbackPort storeDiscoveryFeedbackPort) : IProjectionEventHandler<WorkIgnored>
    {
        public async Task HandleAsync(WorkIgnored @event, CancellationToken cancellationToken = default)
        {
            await storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);
        }
    }
}

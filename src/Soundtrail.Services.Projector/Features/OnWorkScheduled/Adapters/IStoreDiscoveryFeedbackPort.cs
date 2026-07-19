using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled.Adapters;

public interface IStoreDiscoveryFeedbackPort
{
    Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken);
}

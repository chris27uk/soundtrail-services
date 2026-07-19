using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

public interface IStoreDiscoveryFeedbackPort
{
    Task StoreAsync(WorkRequested @event, CancellationToken cancellationToken);

    Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken);

    Task StoreAsync(WorkDeferred @event, CancellationToken cancellationToken);

    Task StoreAsync(WorkCompleted @event, CancellationToken cancellationToken);

    Task StoreAsync(WorkRejected @event, CancellationToken cancellationToken);

    Task StoreAsync(WorkIgnored @event, CancellationToken cancellationToken);

    Task StoreAsync(WorkAttemptFailed @event, CancellationToken cancellationToken);
}

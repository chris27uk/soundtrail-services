using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged;

public sealed class WorkFeedbackChangedProjectorHandler(
    IStoreDiscoveryFeedbackPort storeDiscoveryFeedbackPort)
{
    public Task Handle(WorkRequested @event, CancellationToken cancellationToken = default) =>
        storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);

    public Task Handle(WorkScheduled @event, CancellationToken cancellationToken = default) =>
        storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);

    public Task Handle(WorkDeferred @event, CancellationToken cancellationToken = default) =>
        storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);

    public Task Handle(WorkCompleted @event, CancellationToken cancellationToken = default) =>
        storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);

    public Task Handle(WorkRejected @event, CancellationToken cancellationToken = default) =>
        storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);

    public Task Handle(WorkIgnored @event, CancellationToken cancellationToken = default) =>
        storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);

    public Task Handle(WorkAttemptFailed @event, CancellationToken cancellationToken = default) =>
        storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);
}

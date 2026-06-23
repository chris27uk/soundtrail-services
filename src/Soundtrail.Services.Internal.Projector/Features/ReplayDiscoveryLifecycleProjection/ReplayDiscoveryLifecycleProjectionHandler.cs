using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Internal.Projector.Features.ProjectDiscoveryLifecycle;
using Soundtrail.Services.Internal.Projector.Features.ReplayDiscoveryLifecycleProjection.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.ReplayDiscoveryLifecycleProjection;

public sealed class ReplayDiscoveryLifecycleProjectionHandler(
    ILoadStoredDiscoveryLifecycleEventsPort loadPort,
    ProjectDiscoveryLifecycleHandler projectHandler) : IHandler<ReplayDiscoveryLifecycleProjectionCommand>
{
    public async Task Handle(
        ReplayDiscoveryLifecycleProjectionCommand request,
        CancellationToken cancellationToken = default)
    {
        var events = await loadPort.LoadAsync(request.Criteria, cancellationToken);
        await projectHandler.Handle(new ProjectDiscoveryLifecycleCommand(request.Criteria, events), cancellationToken);
    }
}

using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection.StoredEvents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection;

public sealed class ReplayDiscoveryLifecycleProjectionHandler(
    ILoadStoredDiscoveryLifecycleEventsPort loadPort,
    ProjectDiscoveryLifecycleHandler projectHandler) : IHandler<ReplayDiscoveryLifecycleProjectionCommand, ReplayDiscoveryLifecycleProjectionResult>
{
    public async Task<ReplayDiscoveryLifecycleProjectionResult> Handle(
        ReplayDiscoveryLifecycleProjectionCommand request,
        CancellationToken cancellationToken = default)
    {
        var events = await loadPort.LoadAsync(request.Criteria, cancellationToken);
        await projectHandler.Handle(
            new ProjectDiscoveryLifecycleCommand(request.Criteria, events),
            cancellationToken);
        return new ReplayDiscoveryLifecycleProjectionResult(events.Count);
    }
}

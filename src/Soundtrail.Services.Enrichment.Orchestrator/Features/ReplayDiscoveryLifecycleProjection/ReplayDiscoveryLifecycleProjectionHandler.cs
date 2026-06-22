using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectDiscoveryLifecycle;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayDiscoveryLifecycleProjection.StoredEvents;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayDiscoveryLifecycleProjection;

public sealed class ReplayDiscoveryLifecycleProjectionHandler(
    ILoadStoredDiscoveryLifecycleEventsPort loadPort,
    ProjectDiscoveryLifecycleHandler projectHandler) : IHandler<ReplayDiscoveryLifecycleProjectionCommand>
{
    public async Task Handle(
        ReplayDiscoveryLifecycleProjectionCommand request,
        CancellationToken cancellationToken = default)
    {
        var events = await loadPort.LoadAsync(request.Criteria, cancellationToken);
        await projectHandler.Handle(
            new ProjectDiscoveryLifecycleCommand(request.Criteria, events),
            cancellationToken);
    }
}

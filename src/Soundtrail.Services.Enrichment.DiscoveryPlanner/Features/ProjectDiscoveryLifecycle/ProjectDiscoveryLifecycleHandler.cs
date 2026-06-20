using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle;

public sealed class ProjectDiscoveryLifecycleHandler(
    ILoadDiscoveryLifecycleProjectionPort loadPort,
    ISaveDiscoveryLifecycleProjectionPort savePort) : IHandler<ProjectDiscoveryLifecycleCommand, ProjectDiscoveryLifecycleResult>
{
    public async Task<ProjectDiscoveryLifecycleResult> Handle(
        ProjectDiscoveryLifecycleCommand request,
        CancellationToken cancellationToken = default)
    {
        var projection = await loadPort.LoadAsync(request.Criteria, cancellationToken);

        foreach (var item in request.Events.OrderBy(x => x.Version))
        {
            if (projection.ProjectionVersion >= item.Version)
            {
                continue;
            }

            projection.Apply(item.Event, item.Version);
        }

        await savePort.SaveAsync(projection, cancellationToken);
        return new ProjectDiscoveryLifecycleResult(request.Events.Count);
    }
}

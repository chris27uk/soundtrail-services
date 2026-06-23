using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Internal.Projector.Features.ProjectDiscoveryLifecycle;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;

public sealed class ReplayDiscoveryLifecycleProjectionBatchHandler(
    ILoadDiscoveryLifecycleReplayTargetsPort loadTargetsPort,
    ILoadDiscoveryLifecycleEventsForReplayPort loadEventsPort,
    IResetDiscoveryLifecycleProjectionPort resetPort,
    ProjectDiscoveryLifecycleHandler projectHandler) : IHandler<ReplayDiscoveryLifecycleProjectionBatchCommand>
{
    public async Task Handle(
        ReplayDiscoveryLifecycleProjectionBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        var criteria = await loadTargetsPort.LoadAsync(cancellationToken);

        foreach (var item in criteria)
        {
            await resetPort.ResetAsync(item, cancellationToken);

            var events = await loadEventsPort.LoadAsync(item, cancellationToken);
            if (events.Count == 0)
            {
                continue;
            }

            await projectHandler.Handle(
                new ProjectDiscoveryLifecycleCommand(item, events),
                cancellationToken);
        }
    }
}

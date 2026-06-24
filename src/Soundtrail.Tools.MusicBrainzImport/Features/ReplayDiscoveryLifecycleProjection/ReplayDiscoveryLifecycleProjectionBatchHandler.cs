using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;

namespace Soundtrail.Tools.MusicBrainzImport.Features.OnReplayCatalogSearchStatus;

public sealed class ReplayDiscoveryLifecycleProjectionBatchHandler(
    ILoadDiscoveryLifecycleReplayTargetsPort loadTargetsPort,
    ILoadDiscoveryLifecycleEventsForReplayPort loadEventsPort,
    IResetDiscoveryLifecycleProjectionPort resetPort,
    CatalogSearchStatusChangedHandler projectHandler) : IHandler<ReplayDiscoveryLifecycleProjectionBatchCommand>
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
                new CatalogSearchStatusChangedCommand(item, events),
                cancellationToken);
        }
    }
}

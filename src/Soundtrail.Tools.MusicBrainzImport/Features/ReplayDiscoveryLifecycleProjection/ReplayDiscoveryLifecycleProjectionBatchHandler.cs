using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.EventStore;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.ProjectionReset;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;

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
        var searchTerms = await loadTargetsPort.LoadAsync(cancellationToken);

        foreach (var item in searchTerms)
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

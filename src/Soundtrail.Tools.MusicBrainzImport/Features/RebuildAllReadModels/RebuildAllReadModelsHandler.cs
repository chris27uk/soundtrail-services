using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Operations;
using Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels.OperationalState;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection;

namespace Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels;

public sealed class RebuildAllReadModelsHandler(
    ReplayPlannerMusicTrackProjectionBatchHandler replayPlannerMusicTrackProjectionBatchHandler,
    ReplayCatalogProjectionHandler replayCatalogProjectionHandler,
    ReplayDiscoveryLifecycleProjectionBatchHandler replayDiscoveryLifecycleProjectionBatchHandler,
    IClearPlannerOperationalStatePort clearPlannerOperationalStatePort) : IHandler<RebuildAllReadModelsCommand>
{
    public async Task Handle(
        RebuildAllReadModelsCommand command,
        CancellationToken cancellationToken = default)
    {
        await clearPlannerOperationalStatePort.ClearAsync(cancellationToken);

        await replayPlannerMusicTrackProjectionBatchHandler.Handle(
            new ReplayMusicTrackProjectionBatchCommand(),
            cancellationToken);
        await replayCatalogProjectionHandler.Handle(
            new ReplayCatalogProjectionCommand(),
            cancellationToken);
        await replayDiscoveryLifecycleProjectionBatchHandler.Handle(
            new ReplayDiscoveryLifecycleProjectionBatchCommand(),
            cancellationToken);
    }
}

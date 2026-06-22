using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
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

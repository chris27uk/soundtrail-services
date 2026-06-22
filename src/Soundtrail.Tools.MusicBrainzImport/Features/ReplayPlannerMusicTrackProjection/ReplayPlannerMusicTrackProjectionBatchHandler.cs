using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectMusicTrackProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection;

public sealed class ReplayPlannerMusicTrackProjectionBatchHandler(
    ILoadCatalogProjectionReplayTargetsPort loadTargetsPort,
    ILoadMusicTrackEventsForCatalogReplayPort loadEventsPort,
    IResetPlannerMusicTrackProjectionPort resetPort,
    ProjectMusicTrackProjectionHandler projectHandler) : IHandler<ReplayMusicTrackProjectionBatchCommand, ReplayMusicTrackProjectionBatchResult>
{
    public async Task<ReplayMusicTrackProjectionBatchResult> Handle(
        ReplayMusicTrackProjectionBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        var musicCatalogIds = await loadTargetsPort.LoadAsync(cancellationToken);

        var replayedStreams = 0;
        var replayedEvents = 0;

        foreach (var musicCatalogId in musicCatalogIds)
        {
            await resetPort.ResetAsync(musicCatalogId, cancellationToken);

            var events = await loadEventsPort.LoadAsync(musicCatalogId, cancellationToken);
            if (events.Count == 0)
            {
                continue;
            }

            await projectHandler.Handle(
                new ProjectMusicTrackProjectionCommand(musicCatalogId, events),
                cancellationToken);

            replayedStreams++;
            replayedEvents += events.Count;
        }

        return new ReplayMusicTrackProjectionBatchResult(replayedStreams, replayedEvents);
    }
}

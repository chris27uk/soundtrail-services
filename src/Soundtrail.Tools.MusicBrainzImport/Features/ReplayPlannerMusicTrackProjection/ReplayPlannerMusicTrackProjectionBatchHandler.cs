using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection;

public sealed class ReplayPlannerMusicTrackProjectionBatchHandler(
    ILoadCatalogProjectionReplayTargetsPort loadTargetsPort,
    ILoadMusicTrackEventsForCatalogReplayPort loadEventsPort,
    IResetPlannerMusicTrackProjectionPort resetPort,
    ProjectMusicTrackProjectionHandler projectHandler) : IHandler<ReplayMusicTrackProjectionBatchCommand>
{
    public async Task Handle(
        ReplayMusicTrackProjectionBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        var musicCatalogIds = await loadTargetsPort.LoadAsync(cancellationToken);

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
        }
    }
}

using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;

public sealed class ReplayCatalogProjectionHandler(
    ILoadCatalogProjectionReplayTargetsPort loadTargetsPort,
    ILoadMusicTrackEventsForCatalogReplayPort loadEventsPort,
    IResetCatalogProjectionCheckpointPort resetPort,
    ProjectMusicTrackCatalogHandler projectHandler) : IHandler<ReplayCatalogProjectionCommand, ReplayCatalogProjectionResult>
{
    public async Task<ReplayCatalogProjectionResult> Handle(
        ReplayCatalogProjectionCommand command,
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
                new(musicCatalogId, events),
                cancellationToken);

            replayedStreams++;
            replayedEvents += events.Count;
        }

        return new ReplayCatalogProjectionResult(replayedStreams, replayedEvents);
    }
}

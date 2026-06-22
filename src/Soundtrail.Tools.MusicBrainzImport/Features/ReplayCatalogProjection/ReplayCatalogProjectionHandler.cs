using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackCatalog;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;

public sealed class ReplayCatalogProjectionHandler(
    ILoadCatalogProjectionReplayTargetsPort loadTargetsPort,
    ILoadMusicTrackEventsForCatalogReplayPort loadEventsPort,
    IResetCatalogProjectionCheckpointPort resetPort,
    ProjectMusicTrackCatalogHandler projectHandler) : IHandler<ReplayCatalogProjectionCommand>
{
    public async Task Handle(
        ReplayCatalogProjectionCommand command,
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
                new(musicCatalogId, events),
                cancellationToken);
        }
    }
}

using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.ProjectionReset;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;

public sealed class ReplayCatalogProjectionHandler(
    ILoadCatalogProjectionReplayTargetsPort loadTargetsPort,
    ILoadMusicTrackEventsForCatalogReplayPort loadEventsPort,
    IResetCatalogProjectionCheckpointPort resetPort,
    MusicCatalogChangedHandler projectHandler) : IHandler<ReplayCatalogProjectionCommand>
{
    public async Task Handle(
        ReplayCatalogProjectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var artistIds = await loadTargetsPort.LoadAsync(cancellationToken);

        foreach (var artistId in artistIds)
        {
            await resetPort.ResetAsync(artistId, cancellationToken);

            var events = await loadEventsPort.LoadAsync(artistId, cancellationToken);
            if (events.Count == 0)
            {
                continue;
            }

            await projectHandler.Handle(
                new(artistId, events),
                cancellationToken);
        }
    }
}

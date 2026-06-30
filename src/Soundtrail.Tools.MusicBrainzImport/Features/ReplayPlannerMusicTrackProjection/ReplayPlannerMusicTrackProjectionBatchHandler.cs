using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection.ProjectionReset;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection;

public sealed class ReplayPlannerMusicTrackProjectionBatchHandler(
    ILoadCatalogProjectionReplayTargetsPort loadTargetsPort,
    ILoadMusicTrackEventsForCatalogReplayPort loadEventsPort,
    IResetPlannerMusicTrackProjectionPort resetPort,
    MusicTrackChangedHandler projectHandler) : IHandler<ReplayMusicTrackProjectionBatchCommand>
{
    public async Task Handle(
        ReplayMusicTrackProjectionBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        _ = loadTargetsPort;
        _ = loadEventsPort;
        _ = resetPort;
        _ = projectHandler;
        _ = command;
        _ = cancellationToken;
        await Task.CompletedTask;
    }
}

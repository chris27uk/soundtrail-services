using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection;
using Soundtrail.Services.Internal.Projector.Features.ReplayMusicTrackProjection.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.ReplayMusicTrackProjection;

public sealed class ReplayMusicTrackProjectionHandler(
    ILoadStoredMusicTrackEventsPort loadPort,
    ProjectMusicTrackProjectionHandler projectHandler) : IHandler<ReplayMusicTrackProjectionCommand>
{
    public async Task Handle(
        ReplayMusicTrackProjectionCommand request,
        CancellationToken cancellationToken = default)
    {
        var events = await loadPort.LoadAsync(request.MusicCatalogId, cancellationToken);
        await projectHandler.Handle(new ProjectMusicTrackProjectionCommand(request.MusicCatalogId, events), cancellationToken);
    }
}

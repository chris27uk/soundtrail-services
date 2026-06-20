using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;
using Soundtrail.Services.Catalog.Projector.Features.ReplayMusicTrackCatalogProjection.StoredEvents;

namespace Soundtrail.Services.Catalog.Projector.Features.ReplayMusicTrackCatalogProjection;

public sealed class ReplayMusicTrackCatalogProjectionHandler(
    ILoadStoredMusicTrackEventsPort loadPort,
    ProjectMusicTrackCatalogHandler projectHandler) : IHandler<ReplayMusicTrackCatalogProjectionCommand, ReplayMusicTrackCatalogProjectionResult>
{
    public async Task<ReplayMusicTrackCatalogProjectionResult> Handle(
        ReplayMusicTrackCatalogProjectionCommand request,
        CancellationToken cancellationToken = default)
    {
        var events = await loadPort.LoadAsync(request.MusicCatalogId, cancellationToken);
        await projectHandler.Handle(
            new ProjectMusicTrackCatalogCommand(request.MusicCatalogId, events),
            cancellationToken);
        return new ReplayMusicTrackCatalogProjectionResult(events.Count);
    }
}

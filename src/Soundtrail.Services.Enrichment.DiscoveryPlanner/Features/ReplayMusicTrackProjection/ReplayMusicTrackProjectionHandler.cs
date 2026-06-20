using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectMusicTrackProjection;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayMusicTrackProjection.StoredEvents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayMusicTrackProjection;

public sealed class ReplayMusicTrackProjectionHandler(
    ILoadStoredMusicTrackEventsPort loadPort,
    ProjectMusicTrackProjectionHandler projectHandler) : IHandler<ReplayMusicTrackProjectionCommand, ReplayMusicTrackProjectionResult>
{
    public async Task<ReplayMusicTrackProjectionResult> Handle(
        ReplayMusicTrackProjectionCommand request,
        CancellationToken cancellationToken = default)
    {
        var events = await loadPort.LoadAsync(request.MusicCatalogId, cancellationToken);
        await projectHandler.Handle(
            new ProjectMusicTrackProjectionCommand(request.MusicCatalogId, events),
            cancellationToken);
        return new ReplayMusicTrackProjectionResult(events.Count);
    }
}

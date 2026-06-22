using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectMusicTrackProjection;

public sealed class ProjectMusicTrackProjectionHandler(
    ILoadMusicTrackProjectionPort loadPort,
    ISaveMusicTrackProjectionPort savePort) : IHandler<ProjectMusicTrackProjectionCommand>
{
    public async Task Handle(
        ProjectMusicTrackProjectionCommand request,
        CancellationToken cancellationToken = default)
    {
        var projection = await loadPort.LoadAsync(request.MusicCatalogId, cancellationToken);

        foreach (var item in request.Events.OrderBy(x => x.Version))
        {
            if (projection.ProjectionVersion >= item.Version)
            {
                continue;
            }

            projection.Apply(item.Event, item.Version);
        }

        await savePort.SaveAsync(request.MusicCatalogId, projection, cancellationToken);
    }
}

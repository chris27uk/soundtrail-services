using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackCatalog;

public sealed class ProjectMusicTrackCatalogHandler(
    ILoadMusicTrackCatalogProjectionPort loadPort,
    ISaveMusicTrackCatalogProjectionPort savePort)
{
    public async Task<ProjectMusicTrackCatalogResult> Handle(
        ProjectMusicTrackCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        var projection = await loadPort.LoadAsync(command.MusicCatalogId, cancellationToken);

        foreach (var item in command.Events.OrderBy(x => x.Version))
        {
            if (projection.ProjectionVersion >= item.Version)
            {
                continue;
            }

            projection.Apply(item.Event, item.Version);
        }

        await savePort.SaveAsync(projection, cancellationToken);
        return new ProjectMusicTrackCatalogResult(command.Events.Count);
    }
}

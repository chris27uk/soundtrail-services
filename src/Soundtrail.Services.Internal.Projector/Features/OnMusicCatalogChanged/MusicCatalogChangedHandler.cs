using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged;

public sealed class MusicCatalogChangedHandler(ILoadMusicTrackCatalogProjectionPort loadPort, ISaveMusicTrackCatalogProjectionPort savePort) : IHandler<MusicCatalogChangedCommand>
{
    public async Task Handle(MusicCatalogChangedCommand command, CancellationToken cancellationToken = default)
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
    }
}

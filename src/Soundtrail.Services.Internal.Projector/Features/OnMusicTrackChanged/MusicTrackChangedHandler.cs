using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Ports;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;

public sealed class MusicTrackChangedHandler(
    ILoadMusicTrackProjectionPort loadPort,
    ISaveMusicTrackProjectionPort savePort) : IHandler<MusicTrackChangedCommand>
{
    public async Task Handle(
        MusicTrackChangedCommand request,
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

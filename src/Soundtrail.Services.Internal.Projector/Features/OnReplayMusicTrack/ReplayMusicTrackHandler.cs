using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack;

public sealed class ReplayMusicTrackHandler(ILoadStoredMusicTrackEventsPort loadPort, MusicTrackChangedHandler projectHandler) : IHandler<ReplayMusicTrackCommand>
{
    public async Task Handle(ReplayMusicTrackCommand request, CancellationToken cancellationToken = default)
    {
        var events = await loadPort.LoadAsync(request.MusicCatalogId, cancellationToken);
        await projectHandler.Handle(new MusicTrackChangedCommand(request.MusicCatalogId, events), cancellationToken);
    }
}

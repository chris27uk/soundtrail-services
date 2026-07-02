using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged;

public sealed class MusicCatalogChangedHandler : IHandler<MusicCatalogChangedCommand>
{
    private readonly IEventStreamRepository<ArtistId, IDomainEvent>? repository;
    private readonly ISaveMusicTrackCatalogProjectionPort savePort;

    public MusicCatalogChangedHandler(
        IEventStreamRepository<ArtistId, IDomainEvent> repository,
        ISaveMusicTrackCatalogProjectionPort savePort)
    {
        this.repository = repository;
        this.savePort = savePort;
    }

    public MusicCatalogChangedHandler(ISaveMusicTrackCatalogProjectionPort savePort) => this.savePort = savePort;

    public async Task Handle(MusicCatalogChangedCommand command, CancellationToken cancellationToken = default)
    {
        if (repository is not null)
        {
            var loaded = await repository.LoadAsync(command.ArtistId, cancellationToken);
            var projection = ArtistCatalogProjection.Replay(
                command.ArtistId,
                loaded.Events.Select((@event, index) => new VersionedCatalogEvent(index + 1, @event)).ToArray());
            await savePort.SaveAsync(projection, cancellationToken);
            return;
        }

        var replayProjection = ArtistCatalogProjection.Replay(command.ArtistId, command.Events);
        await savePort.SaveAsync(replayProjection, cancellationToken);
    }
}

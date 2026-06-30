using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Commands;
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

    public MusicCatalogChangedHandler(
        ILoadMusicTrackCatalogProjectionPort loadPort,
        ISaveMusicTrackCatalogProjectionPort savePort)
    {
        _ = loadPort;
        this.savePort = savePort;
    }

    public async Task Handle(MusicCatalogChangedCommand command, CancellationToken cancellationToken = default)
    {
        if (repository is not null)
        {
            var loaded = await ArtistCatalog.LoadAsync(repository, command.ArtistId, cancellationToken);
            await savePort.SaveAsync(command.ArtistId, loaded.Stream.Version, loaded.Aggregate, cancellationToken);
            return;
        }

        var aggregate = new ArtistCatalog();
        foreach (var @event in command.Events.OrderBy(x => x.Version).Select(x => x.Event))
        {
            aggregate.Replay(@event);
        }

        var version = command.Events.Select(x => x.Version).DefaultIfEmpty(0).Max();
        await savePort.SaveAsync(command.ArtistId, version, aggregate, cancellationToken);
    }
}

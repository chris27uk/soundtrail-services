using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumDiscoveryCompleted.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumDiscoveryCompleted;

public sealed class ApplyKnownAlbumDiscoveryCompletedToArtistCatalogHandler(
    IEventStreamRepository<ArtistId, IDomainEvent> catalogRepository) : IHandler<KnownAlbumDiscoveryCompletedCommand>
{
    public async Task Handle(
        KnownAlbumDiscoveryCompletedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events)
        {
            if (item.Event is not KnownAlbumDiscoveryCompleted completed)
            {
                continue;
            }

            var loaded = await ArtistCatalog.LoadAsync(
                catalogRepository,
                completed.ArtistId,
                cancellationToken);

            loaded.Aggregate.DiscoverAlbum(
                completed.ArtistId,
                completed.AlbumId,
                completed.ArtistName,
                completed.AlbumTitle,
                completed.SourceArtistId,
                completed.SourceAlbumId,
                completed.ReleaseDate,
                completed.SourceProvider,
                completed.CompletedAt);

            await loaded.Aggregate.SaveAsync(
                catalogRepository,
                loaded.Stream,
                CommandId.For($"KnownAlbumDiscoveryCompleted:{command.DiscoveryStreamId}:{item.Version}"),
                cancellationToken);
        }
    }
}

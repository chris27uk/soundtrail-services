using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistDiscoveryCompleted.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistDiscoveryCompleted;

public sealed class ApplyKnownArtistDiscoveryCompletedToArtistCatalogHandler(
    IEventStreamRepository<ArtistId, IDomainEvent> catalogRepository) : IHandler<KnownArtistDiscoveryCompletedCommand>
{
    public async Task Handle(
        KnownArtistDiscoveryCompletedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events)
        {
            if (item.Event is not KnownArtistDiscoveryCompleted completed)
            {
                continue;
            }

            var loaded = await ArtistCatalog.LoadAsync(
                catalogRepository,
                completed.ArtistId,
                cancellationToken);

            loaded.Aggregate.DiscoverArtist(
                completed.ArtistId,
                completed.ArtistName,
                completed.SourceArtistId,
                completed.SourceProvider,
                completed.CompletedAt);

            await loaded.Aggregate.SaveAsync(
                catalogRepository,
                loaded.Stream,
                CommandId.For($"KnownArtistDiscoveryCompleted:{command.DiscoveryStreamId}:{item.Version}"),
                cancellationToken);
        }
    }
}

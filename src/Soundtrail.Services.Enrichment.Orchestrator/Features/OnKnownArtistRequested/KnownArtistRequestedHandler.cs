using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Ports;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;

public sealed class KnownArtistRequestedHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository,
    ILoadKnownCatalogArtistPort loadKnownCatalogArtistPort,
    ICommandBus commandBus) : IHandler<KnownArtistRequested>
{
    public async Task Handle(
        KnownArtistRequested request,
        CancellationToken cancellationToken = default)
    {
        var loaded = await KnownItemDiscovery.LoadAsync(
            discoveryRepository,
            KnownCatalogItem.ForArtist(request.ArtistId),
            cancellationToken);

        if (!loaded.Aggregate.ArtistRequested(
                request.ArtistId,
                request.OccurredAt,
                request.CorrelationId))
        {
            return;
        }

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);

        var knownArtist = await loadKnownCatalogArtistPort.LoadAsync(request.ArtistId, cancellationToken)
                          ?? throw new InvalidOperationException(
                              $"Known artist '{request.ArtistId.Value}' must exist in the catalog before lookup can be dispatched.");

        await commandBus.SendAsync(
            new LookupArtistMetadataCommand(
                CommandId.For($"LookupArtistMetadata:{request.ArtistId.Value}"),
                request.ArtistId,
                request.Priority,
                request.OccurredAt,
                request.CorrelationId,
                knownArtist.ArtistName,
                knownArtist.MusicBrainzArtistId),
            cancellationToken);
    }
}

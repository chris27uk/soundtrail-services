using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Ports;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;

public sealed class KnownAlbumRequestedHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository,
    ILoadKnownCatalogAlbumPort loadKnownCatalogAlbumPort,
    ICommandBus commandBus) : IHandler<KnownAlbumRequested>
{
    public async Task Handle(
        KnownAlbumRequested request,
        CancellationToken cancellationToken = default)
    {
        var album = KnownCatalogItem.ForAlbum(request.ArtistId, request.AlbumId);
        var loaded = await KnownItemDiscovery.LoadAsync(
            discoveryRepository,
            album,
            cancellationToken);

        if (!loaded.Aggregate.AlbumRequested(
                request.ArtistId,
                request.AlbumId,
                request.OccurredAt,
                request.CorrelationId))
        {
            return;
        }

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);

        var knownAlbum = await loadKnownCatalogAlbumPort.LoadAsync(request.ArtistId, request.AlbumId, cancellationToken)
                         ?? throw new InvalidOperationException(
                             $"Known album '{request.AlbumId.Value}' for artist '{request.ArtistId.Value}' must exist in the catalog before lookup can be dispatched.");

        await commandBus.SendAsync(
            new LookupAlbumMetadataCommand(
                CommandId.For($"LookupAlbumMetadata:{request.ArtistId.Value}:{request.AlbumId.Value}"),
                request.ArtistId,
                request.AlbumId,
                request.Priority,
                request.OccurredAt,
                request.CorrelationId,
                knownAlbum.ArtistName,
                knownAlbum.AlbumTitle,
                knownAlbum.MusicBrainzReleaseId,
                knownAlbum.MusicBrainzArtistId),
            cancellationToken);
    }
}

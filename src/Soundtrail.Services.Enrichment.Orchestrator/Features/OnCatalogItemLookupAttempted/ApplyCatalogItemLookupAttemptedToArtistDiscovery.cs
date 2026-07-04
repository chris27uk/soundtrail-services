using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted;

public sealed class ApplyCatalogItemLookupAttemptedToArtistDiscovery(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository)
{
    public async Task Handle(
        ArtistId artistId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        MusicCatalogLookupOutcome outcome,
        ArtistMetadataFetched? fetched,
        CancellationToken cancellationToken = default)
    {
        var loaded = await KnownItemDiscovery.LoadAsync(
            discoveryRepository,
            KnownCatalogId.ForArtist(artistId),
            cancellationToken);

        if (fetched is null)
        {
            _ = outcome.Status switch
            {
                MusicCatalogLookupOutcomeStatus.Deferred => loaded.Aggregate.ArtistLookupDeferred(
                    artistId,
                    outcome.RetryAfterSeconds,
                    outcome.RetryAt,
                    outcome.Reason ?? "Lookup deferred",
                    createdAt),
                MusicCatalogLookupOutcomeStatus.Failed => loaded.Aggregate.ArtistLookupFailed(
                    artistId,
                    priority,
                    outcome.Reason ?? "Lookup failed",
                    createdAt),
                MusicCatalogLookupOutcomeStatus.Duplicate => false,
                _ => loaded.Aggregate.ArtistLookupStarted(
                    artistId,
                    priority,
                    "Lookup started",
                    createdAt)
            };
        }
        else
        {
            _ = loaded.Aggregate.ArtistLookupCompleted(
                artistId,
                priority,
                sourceProvider,
                "Discovery completed",
                createdAt,
                fetched.Metadata.ArtistName,
                fetched.Metadata.SourceArtistId);
        }

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}

using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted;

public sealed class ApplyCatalogItemLookupAttemptedToAlbumDiscovery(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository)
{
    public async Task Handle(
        ArtistId artistId,
        AlbumId albumId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        MusicCatalogLookupOutcome outcome,
        AlbumMetadataFetched? fetched,
        CancellationToken cancellationToken = default)
    {
        var loaded = await KnownItemDiscovery.LoadAsync(
            discoveryRepository,
            KnownCatalogId.ForAlbum(artistId, albumId),
            cancellationToken);

        if (fetched is null)
        {
            if (outcome.Status == MusicCatalogLookupOutcomeStatus.Deferred)
            {
                loaded.Aggregate.AlbumLookupDeferred(
                    artistId,
                    albumId,
                    outcome.RetryAfterSeconds,
                    outcome.RetryAt,
                    outcome.Reason ?? "Lookup deferred",
                    createdAt);
            }

            if (outcome.Status == MusicCatalogLookupOutcomeStatus.Failed)
            {
                loaded.Aggregate.AlbumLookupFailed(
                    artistId,
                    albumId,
                    priority,
                    outcome.Reason ?? "Lookup failed",
                    createdAt);
            }

            if (outcome.Status == MusicCatalogLookupOutcomeStatus.Completed)
            {
                loaded.Aggregate.AlbumLookupStarted(
                    artistId,
                    albumId,
                    priority,
                    "Lookup started",
                    createdAt);
            }

            await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
            return;
        }

        loaded.Aggregate.AlbumLookupCompleted(
            artistId,
            albumId,
            priority,
            sourceProvider,
            "Discovery completed",
            createdAt,
            fetched.Metadata.AlbumTitle,
            fetched.Metadata.ArtistName,
            fetched.Metadata.SourceAlbumId,
            fetched.Metadata.SourceArtistId,
            fetched.Metadata.ReleaseDate);

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}

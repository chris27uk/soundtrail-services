using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAlbumMetadataLookupAttempted;

public sealed class AlbumMetadataLookupAttemptedHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<AlbumMetadataLookupAttempted>
{
    public async Task Handle(AlbumMetadataLookupAttempted attempted, CancellationToken cancellationToken = default)
    {
        var loaded = await KnownItemDiscovery.LoadAsync(
            discoveryRepository,
            KnownCatalogItem.ForAlbum(attempted.ArtistId, attempted.AlbumId),
            cancellationToken);

        if (attempted.AlbumMetadataFetched is null)
        {
            if (attempted.Outcome.Status == MusicCatalogLookupOutcomeStatus.Deferred)
            {
                loaded.Aggregate.AlbumLookupDeferred(
                    attempted.ArtistId,
                    attempted.AlbumId,
                    attempted.Outcome.RetryAfterSeconds,
                    attempted.Outcome.RetryAt,
                    attempted.Outcome.Reason ?? "Lookup deferred",
                    attempted.CreatedAt);
            }
            
            
            if (attempted.Outcome.Status == MusicCatalogLookupOutcomeStatus.Failed)
            {
                loaded.Aggregate.AlbumLookupFailed(
                    attempted.ArtistId,
                    attempted.AlbumId,
                    attempted.Priority,
                    attempted.Outcome.Reason ?? "Lookup failed",
                    attempted.CreatedAt);
            }

            if (attempted.Outcome.Status == MusicCatalogLookupOutcomeStatus.Completed)
            {
                loaded.Aggregate.AlbumLookupStarted(
                    attempted.ArtistId,
                    attempted.AlbumId,
                    attempted.Priority,
                    "Lookup started",
                    attempted.CreatedAt);
            }
          
            await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
            return;
        }
        
        loaded.Aggregate.AlbumLookupCompleted(
            attempted.ArtistId,
            attempted.AlbumId,
            attempted.Priority,
            attempted.SourceProvider,
            "Discovery completed",
            attempted.CreatedAt,
            attempted.AlbumMetadataFetched.Metadata.AlbumTitle,
            attempted.AlbumMetadataFetched.Metadata.ArtistName,
            attempted.AlbumMetadataFetched.Metadata.SourceAlbumId,
            attempted.AlbumMetadataFetched.Metadata.SourceArtistId,
            attempted.AlbumMetadataFetched.Metadata.ReleaseDate);

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}

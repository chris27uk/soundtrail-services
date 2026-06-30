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

        var changed = attempted.AlbumMetadataFetched is not null
            ? loaded.Aggregate.AlbumLookupCompleted(
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
                attempted.AlbumMetadataFetched.Metadata.ReleaseDate)
            : attempted.Outcome.Status switch
            {
                MusicCatalogLookupOutcomeStatus.Deferred => loaded.Aggregate.AlbumLookupDeferred(
                    attempted.ArtistId,
                    attempted.AlbumId,
                    attempted.Outcome.RetryAfterSeconds,
                    attempted.Outcome.RetryAt,
                    attempted.Outcome.Reason ?? "Lookup deferred",
                    attempted.CreatedAt),
                MusicCatalogLookupOutcomeStatus.Failed => loaded.Aggregate.AlbumLookupFailed(
                    attempted.ArtistId,
                    attempted.AlbumId,
                    attempted.Priority,
                    attempted.Outcome.Reason ?? "Lookup failed",
                    attempted.CreatedAt),
                MusicCatalogLookupOutcomeStatus.Duplicate => false,
                _ => loaded.Aggregate.AlbumLookupStarted(
                    attempted.ArtistId,
                    attempted.AlbumId,
                    attempted.Priority,
                    "Lookup started",
                    attempted.CreatedAt)
            };

        if (!changed)
        {
            return;
        }

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}

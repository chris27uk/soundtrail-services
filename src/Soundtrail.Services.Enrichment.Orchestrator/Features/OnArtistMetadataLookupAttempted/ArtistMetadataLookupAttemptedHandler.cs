using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnArtistMetadataLookupAttempted;

public sealed class ArtistMetadataLookupAttemptedHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<ArtistMetadataLookupAttempted>
{
    public async Task Handle(ArtistMetadataLookupAttempted attempted, CancellationToken cancellationToken = default)
    {
        var loaded = await KnownItemDiscovery.LoadAsync(
            discoveryRepository,
            KnownCatalogItem.ForArtist(attempted.ArtistId),
            cancellationToken);

        if (attempted.ArtistMetadataFetched is null)
        {
            _ = attempted.Outcome.Status switch
            {
                MusicCatalogLookupOutcomeStatus.Deferred => loaded.Aggregate.ArtistLookupDeferred(
                    attempted.ArtistId,
                    attempted.Outcome.RetryAfterSeconds,
                    attempted.Outcome.RetryAt,
                    attempted.Outcome.Reason ?? "Lookup deferred",
                    attempted.CreatedAt),
                MusicCatalogLookupOutcomeStatus.Failed => loaded.Aggregate.ArtistLookupFailed(
                    attempted.ArtistId,
                    attempted.Priority,
                    attempted.Outcome.Reason ?? "Lookup failed",
                    attempted.CreatedAt),
                MusicCatalogLookupOutcomeStatus.Duplicate => false,
                _ => loaded.Aggregate.ArtistLookupStarted(
                    attempted.ArtistId,
                    attempted.Priority,
                    "Lookup started",
                    attempted.CreatedAt)
            };
        }
        else
        {
            _ = loaded.Aggregate.ArtistLookupCompleted(
                attempted.ArtistId,
                attempted.Priority,
                attempted.SourceProvider,
                "Discovery completed",
                attempted.CreatedAt,
                attempted.ArtistMetadataFetched.Metadata.ArtistName,
                attempted.ArtistMetadataFetched.Metadata.SourceArtistId);
        }

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}

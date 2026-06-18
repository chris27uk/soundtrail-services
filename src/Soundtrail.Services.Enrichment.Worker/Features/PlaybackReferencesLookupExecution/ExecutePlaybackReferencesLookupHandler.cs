using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;

public sealed class ExecutePlaybackReferencesLookupHandler(
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    IGetMusicTrackReference getMusicTrackReference,
    IReserveSourceApiBudgetPort reserveSourceApiBudgetPort,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository)
{
    private static readonly ProviderName[] SupportedPlaybackProviders =
    [
        ProviderName.AppleMusic,
        ProviderName.Spotify,
        ProviderName.YoutubeMusic
    ];

    public async Task<LookupExecutionResult> Handle(
        ResolvePlaybackReferencesCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var idempotencySession = await IdempotencySession.StartAsync(lookupExecutionReceiptStore, command.CommandId, cancellationToken);

        if (idempotencySession.ProcessedBefore)
        {
            return LookupExecutionResult.Duplicate();
        }

        var reservation = await reserveSourceApiBudgetPort.TryReserveAsync(
            new SourceApiBudgetReservationRequest(command.TargetProvider, command.CreatedAt),
            cancellationToken);

        if (!reservation.Accepted)
        {
            await UpdateDiscoveryAsync(
                command.MusicCatalogId,
                discovery => discovery.Defer(
                    reservation.RetryAfterSecondsFrom(command.CreatedAt),
                    reservation.RetryAt,
                    reservation.Reason,
                    command.CreatedAt),
                cancellationToken);
            return LookupExecutionResult.Deferred();
        }

        await UpdateDiscoveryAsync(
            command.MusicCatalogId,
            discovery => discovery.Start(command.Priority, "Lookup started", command.CreatedAt),
            cancellationToken);

        try
        {
            var references = await getMusicTrackReference.GetReferenceToMusicTrack(command.LookupKey, cancellationToken);
            var failures = SupportedPlaybackProviders
                .Where(provider => references.All(reference => reference.Provider != provider))
                .Select(provider => new ProviderLookupFailure(provider, command.TargetProvider))
                .ToArray();
            return LookupExecutionResult.Completed(command.ToEnrichmentResponse(references, failures));
        }
        catch
        {
            await UpdateDiscoveryAsync(
                command.MusicCatalogId,
                discovery => discovery.Fail("Lookup failed", command.CreatedAt),
                cancellationToken);
            throw;
        }
    }

    private async Task UpdateDiscoveryAsync(
        MusicCatalogId musicCatalogId,
        Func<CatalogSearchDiscovery, bool> transition,
        CancellationToken cancellationToken)
    {
        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, tracking.Criteria, cancellationToken);
            transition(discovery);
            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }
    }
}

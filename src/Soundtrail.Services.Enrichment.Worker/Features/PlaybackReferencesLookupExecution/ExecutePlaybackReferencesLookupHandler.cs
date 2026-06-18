using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;

public sealed class ExecutePlaybackReferencesLookupHandler(
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    IGetMusicTrackReference getMusicTrackReference,
    IReserveSourceApiBudgetPort reserveSourceApiBudgetPort)
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
            return LookupExecutionResult.Deferred(
                reservation.Reason,
                reservation.RetryAt,
                reservation.RetryAfterSecondsFrom(command.CreatedAt));
        }

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
            return LookupExecutionResult.Failed("Lookup failed");
        }
    }
}

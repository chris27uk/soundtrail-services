using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Features.Orchestration;

public sealed class EnrichmentOrchestrator(
    IActiveLookupWorkStore activeLookupWorkStore)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public async Task<EnrichmentOrchestrationResult> PlanAsync(
        TrackEnrichmentState previousState,
        TrackEnrichmentState currentState,
        EnrichmentResponse response,
        CancellationToken cancellationToken = default)
    {
        var events = BuildEvents(previousState, currentState, response);
        var commands = new List<IEnrichmentIntentCommand>();

        if (response.SourceProvider == ProviderName.MusicBrainz
            && currentState.CanonicalMetadata is not null)
        {
            await TryScheduleVerificationAsync(
                previousState.Apple,
                currentState.Apple,
                () => new VerifyApplePlaybackReferenceCommand(
                    CommandId.For($"VerifyApplePlaybackReference:{response.MusicCatalogId.Value}"),
                    response.MusicCatalogId,
                    response.Priority,
                    response.CreatedAt,
                    response.CorrelationId),
                () => new ApplePlaybackVerificationRequested(response.MusicCatalogId, response.CorrelationId),
                commands,
                events,
                cancellationToken);

            await TryScheduleVerificationAsync(
                previousState.YouTubeMusic,
                currentState.YouTubeMusic,
                () => new VerifyYouTubeMusicPlaybackReferenceCommand(
                    CommandId.For($"VerifyYouTubeMusicPlaybackReference:{response.MusicCatalogId.Value}"),
                    response.MusicCatalogId,
                    response.Priority,
                    response.CreatedAt,
                    response.CorrelationId),
                () => new YouTubeMusicPlaybackVerificationRequested(response.MusicCatalogId, response.CorrelationId),
                commands,
                events,
                cancellationToken);
        }

        return new EnrichmentOrchestrationResult(commands, events);
    }

    private async Task TryScheduleVerificationAsync(
        ProviderReference? previousReference,
        ProviderReference? currentReference,
        Func<IEnrichmentIntentCommand> createCommand,
        Func<IEnrichmentOrchestrationEvent> createRequestedEvent,
        List<IEnrichmentIntentCommand> commands,
        List<IEnrichmentOrchestrationEvent> events,
        CancellationToken cancellationToken)
    {
        if (!ShouldVerify(previousReference, currentReference))
        {
            return;
        }

        var command = createCommand();
        var acquired = await activeLookupWorkStore.TryAcquireAsync(
            command.CommandId,
            command.CreatedAt.Add(ActiveReservationDuration),
            cancellationToken);
        if (!acquired)
        {
            return;
        }

        commands.Add(command);
        events.Add(createRequestedEvent());
    }

    private static List<IEnrichmentOrchestrationEvent> BuildEvents(
        TrackEnrichmentState previousState,
        TrackEnrichmentState currentState,
        EnrichmentResponse response)
    {
        var events = new List<IEnrichmentOrchestrationEvent>();

        if (response.SourceProvider == ProviderName.MusicBrainz
            && response.Metadata is not null
            && previousState.CanonicalMetadata is null
            && currentState.CanonicalMetadata is not null)
        {
            events.Add(new CanonicalMetadataResolved(
                response.MusicCatalogId,
                currentState.CanonicalMetadata,
                response.CorrelationId));
        }

        foreach (var reference in response.References)
        {
            if (reference.Confidence == ReferenceConfidence.Verified)
            {
                events.Add(new ProviderReferenceVerified(
                    response.MusicCatalogId,
                    reference.Provider,
                    reference.ExternalId,
                    response.CorrelationId));
                continue;
            }

            events.Add(new ProviderReferenceDiscovered(
                response.MusicCatalogId,
                reference.Provider,
                reference.ExternalId,
                response.SourceProvider,
                response.CorrelationId));
        }

        if (!previousState.IsPlayable && currentState.IsPlayable)
        {
            events.Add(new TrackBecamePlayable(response.MusicCatalogId, response.CorrelationId));
            events.Add(new EnrichmentCompleted(response.MusicCatalogId, response.CorrelationId));
        }

        return events;
    }

    private static bool ShouldVerify(
        ProviderReference? previousReference,
        ProviderReference? currentReference)
    {
        if (currentReference is null)
        {
            return false;
        }

        if (currentReference.Confidence == ReferenceConfidence.Verified)
        {
            return false;
        }

        if (currentReference.SourceProvider == currentReference.Provider)
        {
            return false;
        }

        return previousReference?.Confidence != ReferenceConfidence.Verified;
    }
}

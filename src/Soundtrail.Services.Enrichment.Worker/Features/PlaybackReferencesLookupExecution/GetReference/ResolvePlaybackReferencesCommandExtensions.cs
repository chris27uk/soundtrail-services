using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;

internal static class ResolvePlaybackReferencesCommandExtensions
{
    public static EnrichmentResponse ToEnrichmentResponse(
        this ResolvePlaybackReferencesCommand command,
        IReadOnlyList<ExternalReference> references,
        IReadOnlyList<ProviderLookupFailure> failedProviders) =>
        new(
            command.CommandId,
            command.MusicCatalogId,
            command.TargetProvider,
            command.Priority,
            command.CreatedAt,
            null,
            references,
            failedProviders,
            command.Hierarchy,
            command.CorrelationId);
}

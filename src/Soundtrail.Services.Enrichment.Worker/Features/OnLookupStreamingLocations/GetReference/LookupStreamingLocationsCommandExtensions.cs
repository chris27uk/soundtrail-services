using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;

internal static class LookupStreamingLocationsCommandExtensions
{
    public static MusicCatalogMetadataFetched ToMusicCatalogMetadataFetched(
        this LookupStreamingLocationsCommand command,
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

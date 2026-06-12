using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Lookup;

internal static class LookupCanonicalMusicMetadataCommandExtensions
{
    public static EnrichmentResponse ToEnrichmentResponse(
        this LookupMusicMetadataCommand command,
        SongMetadata? metadata) =>
        new(
            command.CommandId,
            command.MusicCatalogId,
            ProviderName.MusicBrainz,
            command.Priority,
            command.CreatedAt,
            metadata,
            [],
            command.CorrelationId);
}

using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Responses;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

public sealed class EnrichmentResponseListener(ApplyEnrichmentResponseHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        EnrichmentResponseDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        await handler.Handle(
            new EnrichmentResponse(
                CommandId.From(dto.CommandId),
                MusicCatalogId.From(dto.MusicCatalogId),
                ProviderName.From(dto.SourceProvider),
                dto.Priority,
                dto.CreatedAt,
                dto.Metadata is null
                    ? null
                    : new SongMetadata(
                        dto.Metadata.Title,
                        dto.Metadata.Artist,
                        dto.Metadata.Isrc,
                        dto.Metadata.Mbid,
                        dto.Metadata.DurationMs),
                dto.References.Select(reference => new ExternalReference(
                    ProviderName.From(reference.Provider),
                    reference.Url,
                    reference.ExternalId,
                    Enum.Parse<ReferenceConfidence>(reference.Confidence, ignoreCase: true))).ToArray(),
                CorrelationId.From(dto.CorrelationId)),
            cancellationToken);

        return [];
    }
}

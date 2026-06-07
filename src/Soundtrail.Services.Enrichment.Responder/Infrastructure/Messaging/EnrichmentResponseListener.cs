using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Worker.Responses;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Responder.Infrastructure.Messaging;

public sealed class EnrichmentResponseListener(ApplyEnrichmentResponseHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        EnrichmentResponseDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(
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
                    reference.ConfidenceDto)).ToArray(),
                CorrelationId.From(dto.CorrelationId)),
            cancellationToken);
}

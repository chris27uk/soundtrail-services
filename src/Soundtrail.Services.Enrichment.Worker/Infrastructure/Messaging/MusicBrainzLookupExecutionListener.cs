using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Orchestrator;
using Soundtrail.Contracts.Worker;
using Soundtrail.Contracts.Worker.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.Worker.Features.Execution;
using Soundtrail.Services.Enrichment.Worker.Features.Execution.MusicBrainzLookupExecution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class MusicBrainzLookupExecutionListener(ExecuteMusicBrainzLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        ResolveCanonicalMetadataCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(
            new Features.Execution.ResolveCanonicalMetadataCommand(
                CommandId.From(dto.CommandId),
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                dto.CreatedAt,
                CorrelationId.From(dto.CorrelationId)),
            cancellationToken);
        return result.Response is null
            ? []
            : [new EnrichmentResponseDto(
                result.Response.CommandId,
                result.Response.MusicCatalogId,
                result.Response.SourceProvider.Value,
                result.Response.Priority,
                result.Response.CreatedAt,
                result.Response.Metadata is null
                    ? null
                    : new SongMetadataDto(
                        result.Response.Metadata.Title,
                        result.Response.Metadata.Artist,
                        result.Response.Metadata.Isrc,
                        result.Response.Metadata.Mbid,
                        result.Response.Metadata.DurationMs),
                result.Response.References.Select(reference => new ExternalReferenceDto(
                    reference.Provider.Value,
                    reference.Url,
                    reference.ExternalId,
                    (ReferenceConfidenceDto)reference.Confidence)).ToArray(),
                result.Response.CorrelationId)];
    }
}

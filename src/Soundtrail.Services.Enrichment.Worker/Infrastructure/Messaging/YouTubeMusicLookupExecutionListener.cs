using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Worker.Responses;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.Execution;
using Soundtrail.Services.Enrichment.Worker.Features.Execution.YouTubeMusicLookupExecution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class YouTubeMusicLookupExecutionListener(ExecuteYouTubeMusicLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        ResolveYouTubeMusicPlaybackReferenceCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(
            new ResolveYouTubeMusicPlaybackReferenceCommand(
                CommandId.From(dto.CommandId),
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                dto.CreatedAt,
                CorrelationId.From(dto.CorrelationId)),
            cancellationToken);
        return result.Response is null
            ? []
            : [new EnrichmentResponseDto(
                result.Response.CommandId.Value,
                result.Response.MusicCatalogId.Value,
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
                    reference.Confidence.ToString())).ToArray(),
                result.Response.CorrelationId.Value)];
    }
}

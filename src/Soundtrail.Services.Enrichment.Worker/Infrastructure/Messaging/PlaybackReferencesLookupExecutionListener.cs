using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Responses;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class PlaybackReferencesLookupExecutionListener(ExecutePlaybackReferencesLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        ResolvePlaybackReferencesCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(
            new ResolvePlaybackReferencesCommand(
                CommandId.From(dto.CommandId),
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                dto.CreatedAt,
                CorrelationId.From(dto.CorrelationId),
                dto.SearchTerm.Isrc == null ? MusicSearchTerm.ByTrackArtistAlbum(dto.SearchTerm.Title!, dto.SearchTerm.Artist!,dto.SearchTerm.Album) : MusicSearchTerm.ByIsrc(dto.SearchTerm.Isrc)),
            cancellationToken);

        return result.Response is null
            ? []
            : [new EnrichmentResponseDto(
                result.Response.CommandId.Value,
                result.Response.MusicCatalogId.Value,
                result.Response.SourceProvider.Value,
                result.Response.Priority,
                result.Response.CreatedAt,
                null,
                result.Response.References.Select(reference => new ExternalReferenceDto(
                    reference.Provider.Value,
                    reference.Url,
                    reference.ExternalId,
                    reference.Confidence.ToString())).ToArray(),
                result.Response.CorrelationId.Value)];
    }
}

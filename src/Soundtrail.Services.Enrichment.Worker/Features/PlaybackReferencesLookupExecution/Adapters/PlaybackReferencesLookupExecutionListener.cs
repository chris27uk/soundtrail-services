using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;

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
                dto.SearchTerm.Isrc == null ? MusicSearchTerm.ByTrackArtistAlbum(dto.SearchTerm.Title!, dto.SearchTerm.Artist!, dto.SearchTerm.Album) : MusicSearchTerm.ByIsrc(dto.SearchTerm.Isrc),
                ToHierarchy(dto)),
            cancellationToken);

        var command = new ResolvePlaybackReferencesCommand(
            CommandId.From(dto.CommandId),
            MusicCatalogId.From(dto.MusicCatalogId),
            dto.Priority,
            dto.CreatedAt,
            CorrelationId.From(dto.CorrelationId),
            dto.SearchTerm.Isrc == null ? MusicSearchTerm.ByTrackArtistAlbum(dto.SearchTerm.Title!, dto.SearchTerm.Artist!, dto.SearchTerm.Album) : MusicSearchTerm.ByIsrc(dto.SearchTerm.Isrc),
            ToHierarchy(dto));
        var messages = new List<object>();
        if (result.Response is not null)
        {
            messages.Add(new EnrichmentResponseDto(
                result.Response.CommandId.Value,
                result.Response.MusicCatalogId.Value,
                result.Response.SourceProvider.Value,
                result.Response.Priority,
                result.Response.CreatedAt,
                null,
                result.Response.References.Select(reference => new ExternalReferenceDto(
                    reference.Provider.Value,
                    reference.Url,
                    reference.ExternalId)).ToArray(),
                result.Response.FailedProviders.Select(failure => new ProviderLookupFailureDto(
                    failure.Provider.Value,
                    failure.SourceProvider.Value)).ToArray(),
                result.Response.Hierarchy?.ArtistId?.Value,
                result.Response.Hierarchy?.AlbumId?.Value,
                result.Response.CorrelationId.Value));
        }

        if (result.Outcome is LookupExecutionOutcome.Deferred or LookupExecutionOutcome.Failed)
        {
            messages.Add(result.ToReport(command, ProviderName.Odesli.Value));
        }

        return messages.ToArray();
    }

    private static CatalogTrackHierarchy? ToHierarchy(ResolvePlaybackReferencesCommandDto dto) =>
        dto.ArtistId is null && dto.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId));
}

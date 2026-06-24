using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Wolverine;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;

public sealed class PlaybackReferencesLookupExecutionListener(ExecutePlaybackReferencesLookupHandler handler, IMessageBus messageBus)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
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
        await messageBus.SendAsync(result.ToDto(command, ProviderName.Odesli.Value));
    }

    private static CatalogTrackHierarchy? ToHierarchy(ResolvePlaybackReferencesCommandDto dto) =>
        dto.ArtistId is null && dto.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId));
}

using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;
using Wolverine;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Adapters;

public sealed class LookupMusicMetadataListener(
    ILookupMusicMetadataHandler handler,
    IMessageBus messageBus)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
        LookupMusicMetadataCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var command = new LookupMusicMetadataCommand(
            CommandId.From(dto.CommandId),
            MusicCatalogId.From(dto.MusicCatalogId),
            dto.Priority,
            dto.CreatedAt,
            CorrelationId.From(dto.CorrelationId),
            ToSearchTerm(dto),
            ToHierarchy(dto));
        var result = await handler.Handle(command, cancellationToken);
        await messageBus.SendAsync(result.ToDto(command, ProviderName.MusicBrainz.Value));
    }

    private static MusicSearchTerm ToSearchTerm(LookupMusicMetadataCommandDto dto) =>
        dto.SearchKind switch
        {
            MusicSearchKind.UnifiedSearch => MusicSearchTerm.ByQuery(
                dto.Query ?? throw new InvalidOperationException("Unified music metadata lookup requires a query.")),
            MusicSearchKind.Isrc => MusicSearchTerm.ByIsrc(
                dto.Isrc ?? throw new InvalidOperationException("ISRC music metadata lookup requires an ISRC.")),
            MusicSearchKind.TrackArtistAlbum => MusicSearchTerm.ByTrackArtistAlbum(
                dto.TrackName ?? throw new InvalidOperationException("Track/artist/album music metadata lookup requires a track name."),
                dto.ArtistName ?? throw new InvalidOperationException("Track/artist/album music metadata lookup requires an artist name."),
                dto.AlbumName),
            _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.SearchKind}'.")
        };

    private static CatalogTrackHierarchy? ToHierarchy(LookupMusicMetadataCommandDto dto) =>
        dto.ArtistId is null && dto.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId));
}

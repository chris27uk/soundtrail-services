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
using Wolverine;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupCanonicalMusicMetadata.Adapters;

public sealed class LookupCanonicalMusicMetadataListener(
    LookupCanonicalMusicMetadataHandler handler,
    IMessageBus messageBus)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
        LookupCanonicalMusicMetadataCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(
            new LookupMusicMetadataCommand(
                CommandId.From(dto.CommandId),
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                dto.CreatedAt,
                CorrelationId.From(dto.CorrelationId),
                ToSearchTerm(dto),
                ToHierarchy(dto)),
            cancellationToken);

        var command = new LookupMusicMetadataCommand(
            CommandId.From(dto.CommandId),
            MusicCatalogId.From(dto.MusicCatalogId),
            dto.Priority,
            dto.CreatedAt,
            CorrelationId.From(dto.CorrelationId),
            ToSearchTerm(dto),
            ToHierarchy(dto));
        await messageBus.SendAsync(result.ToDto(command, ProviderName.MusicBrainz.Value));
    }

    private static MusicSearchTerm ToSearchTerm(LookupCanonicalMusicMetadataCommandDto dto) =>
        !string.IsNullOrWhiteSpace(dto.Isrc)
            ? MusicSearchTerm.ByIsrc(dto.Isrc)
            : MusicSearchTerm.ByTrackArtistAlbum(
                dto.TrackName ?? string.Empty,
                dto.ArtistName ?? string.Empty,
                dto.AlbumName);

    private static CatalogTrackHierarchy? ToHierarchy(LookupCanonicalMusicMetadataCommandDto dto) =>
        dto.ArtistId is null && dto.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId));
}

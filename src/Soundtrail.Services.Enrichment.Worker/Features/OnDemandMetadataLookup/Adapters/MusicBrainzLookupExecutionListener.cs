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

namespace Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;

public sealed class MusicBrainzLookupExecutionListener(
    OnDemandLookupMetadataHandler handler,
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
        var messages = new List<object>();
        if (result.Response is not null)
        {
            messages.Add(new EnrichmentResponseDto(
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
                        result.Response.Metadata.DurationMs,
                        result.Response.Metadata.AlbumTitle,
                        result.Response.Metadata.ReleaseDate,
                        result.Response.Metadata.SourceArtistId,
                        result.Response.Metadata.SourceAlbumId),
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
            messages.Add(result.ToReport(command, ProviderName.MusicBrainz.Value));
        }

        foreach (var message in messages)
        {
            await messageBus.SendAsync(message);
        }
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

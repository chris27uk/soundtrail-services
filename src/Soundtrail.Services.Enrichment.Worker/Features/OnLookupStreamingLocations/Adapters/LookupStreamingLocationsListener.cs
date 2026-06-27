using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Pipeline;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Wolverine;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;

public sealed class LookupStreamingLocationsListener(ILookupStreamingLocationsHandler handler, IMessageBus messageBus)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
        LookupStreamingLocationsCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var command = new LookupStreamingLocationsCommand(
            CommandId.From(dto.CommandId),
            MusicCatalogId.From(dto.MusicCatalogId),
            dto.Priority,
            dto.CreatedAt,
            CorrelationId.From(dto.CorrelationId),
            ToSearchTerm(dto.SearchTerm),
            ToHierarchy(dto));
        var result = await handler.Handle(command, cancellationToken);
        await messageBus.SendAsync(result.ToDto(command, ProviderName.Odesli.Value));
    }

    private static MusicSearchCriteria ToSearchTerm(StreamingLocationSearchTermDto dto) =>
        dto.Kind switch
        {
            MusicSearchKind.UnifiedSearch => MusicSearchCriteria.ByQuery(
                dto.Query ?? throw new InvalidOperationException("Unified streaming locations lookup requires a query.")),
            MusicSearchKind.Isrc => MusicSearchCriteria.ByIsrc(
                dto.Isrc ?? throw new InvalidOperationException("ISRC streaming locations lookup requires an ISRC.")),
            MusicSearchKind.TrackArtistAlbum => MusicSearchCriteria.ByTrackArtistAlbum(
                dto.Title ?? throw new InvalidOperationException("Track/artist/album streaming locations lookup requires a title."),
                dto.Artist ?? throw new InvalidOperationException("Track/artist/album streaming locations lookup requires an artist."),
                dto.Album),
            _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.Kind}'.")
        };

    private static CatalogTrackHierarchy? ToHierarchy(LookupStreamingLocationsCommandDto dto) =>
        dto.ArtistId is null && dto.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId));
}

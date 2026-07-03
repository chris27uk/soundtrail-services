using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicCatalogItem;

public sealed class AssessMusicCatalogItemHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository,
    IDiscoveryAssessmentPolicy discoveryPriorityPolicy,
    ILocalMusicTrackSearch localMusicTrackSearch) : IHandler<AssessMusicCatalogItemCommand>
{
    public async Task Handle(AssessMusicCatalogItemCommand command, CancellationToken cancellationToken = default)
    {
        switch (command)
        {
            case
            {
                ItemId: CatalogItemId.Track(var trackId),
                Resource: CatalogItemResource.SearchCriteria(var searchCriteria)
            }:
                await AssessTrackSearchCandidate(
                    trackId,
                    searchCriteria,
                    command.CreatedAt,
                    cancellationToken);
                return;

            case
            {
                ItemId: CatalogItemId.Artist(var artistId),
                Resource: CatalogItemResource.SearchCriteria(var searchCriteria)
            }:
                await RequestArtistLookupFromSearch(
                    artistId,
                    searchCriteria,
                    command.CreatedAt,
                    command.CorrelationId,
                    cancellationToken);
                return;

            case
            {
                ItemId: CatalogItemId.Album(var albumId),
                Resource: CatalogItemResource.SearchCriteria(var searchCriteria)
            }:
                await RequestAlbumLookupFromSearch(
                    albumId,
                    searchCriteria,
                    command.CreatedAt,
                    command.CorrelationId,
                    cancellationToken);
                return;

            case
            {
                ItemId: CatalogItemId.Artist(var artistId),
                Resource: CatalogItemResource.CatalogItem(var resourceItemId)
            }:
                await RequestArtistLookupFromCatalogItem(
                    artistId,
                    resourceItemId,
                    command.CreatedAt,
                    command.CorrelationId,
                    cancellationToken);
                return;

            case
            {
                ItemId: CatalogItemId.Album(var albumId),
                Resource: CatalogItemResource.CatalogItem(var resourceItemId)
            }:
                await RequestAlbumLookupFromCatalogItem(
                    albumId,
                    resourceItemId,
                    command.CreatedAt,
                    command.CorrelationId,
                    cancellationToken);
                return;

            case
            {
                ItemId: CatalogItemId.Track(var trackId),
                Resource: CatalogItemResource.CatalogItem(var resourceItemId)
            }:
                await RequestTrackLookupFromCatalogItem(
                    trackId,
                    resourceItemId,
                    command.CreatedAt,
                    command.CorrelationId,
                    cancellationToken);
                return;

            default:
                throw new InvalidOperationException(
                    $"Unsupported assessment command shape for '{command.ItemId.EntityKind}'.");
        }
    }

    private async Task AssessTrackSearchCandidate(
        TrackId trackId,
        MusicSearchCriteria searchCriteria,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var loaded = await SearchDiscoveryHistory.LoadAsync(
            discoveryRepository,
            searchCriteria,
            cancellationToken);

        var musicCatalogId = MusicCatalogId.From(trackId.Value);
        var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        loaded.Aggregate.Assess(
            discoveryPriorityPolicy,
            createdAt,
            localTrack?.IsPlayable == true,
            musicCatalogId);

        await loaded.Aggregate.SaveAsync(
            discoveryRepository,
            loaded.Stream,
            cancellationToken);
    }

    private async Task RequestArtistLookupFromSearch(
        ArtistId artistId,
        MusicSearchCriteria searchCriteria,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken)
    {
        var loaded = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            searchCriteria,
            cancellationToken);

        loaded.Aggregate.ArtistCatalogLookupRequested(
            artistId,
            createdAt,
            correlationId);

        await loaded.Aggregate.SaveAsync(
            discoveryRepository,
            loaded.Stream,
            cancellationToken);
    }

    private async Task RequestAlbumLookupFromSearch(
        CatalogAlbumId albumId,
        MusicSearchCriteria searchCriteria,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken)
    {
        var loaded = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            searchCriteria,
            cancellationToken);

        loaded.Aggregate.AlbumCatalogLookupRequested(
            albumId.ArtistId,
            albumId.AlbumId,
            createdAt,
            correlationId);

        await loaded.Aggregate.SaveAsync(
            discoveryRepository,
            loaded.Stream,
            cancellationToken);
    }

    private async Task RequestArtistLookupFromCatalogItem(
        ArtistId artistId,
        CatalogItemId resourceItemId,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken)
    {
        var loaded = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            ToKnownCatalogId(resourceItemId),
            cancellationToken);

        loaded.Aggregate.ArtistCatalogLookupRequested(
            artistId,
            createdAt,
            correlationId);

        await loaded.Aggregate.SaveAsync(
            discoveryRepository,
            loaded.Stream,
            cancellationToken);
    }

    private async Task RequestAlbumLookupFromCatalogItem(
        CatalogAlbumId albumId,
        CatalogItemId resourceItemId,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken)
    {
        var loaded = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            ToKnownCatalogId(resourceItemId),
            cancellationToken);

        loaded.Aggregate.AlbumCatalogLookupRequested(
            albumId.ArtistId,
            albumId.AlbumId,
            createdAt,
            correlationId);

        await loaded.Aggregate.SaveAsync(
            discoveryRepository,
            loaded.Stream,
            cancellationToken);
    }

    private async Task RequestTrackLookupFromCatalogItem(
        TrackId trackId,
        CatalogItemId resourceItemId,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken)
    {
        var loaded = await SearchOrSeekHistory.LoadAsync(
            discoveryRepository,
            ToKnownCatalogId(resourceItemId),
            cancellationToken);

        loaded.Aggregate.KnownTrackRequested(
            trackId,
            PlaybackProviderFilter.Empty,
            createdAt,
            correlationId);

        await loaded.Aggregate.SaveAsync(
            discoveryRepository,
            loaded.Stream,
            cancellationToken);
    }

    private static KnownCatalogId ToKnownCatalogId(CatalogItemId itemId) =>
        itemId switch
        {
            CatalogItemId.Track(var trackId) => KnownCatalogId.ForTrack(trackId),
            CatalogItemId.Artist(var artistId) => KnownCatalogId.ForArtist(artistId),
            CatalogItemId.Album(var albumId) => KnownCatalogId.ForAlbum(albumId.ArtistId, albumId.AlbumId),
            _ => throw new InvalidOperationException($"Unsupported catalog item id '{itemId.GetType().Name}'.")
        };
}

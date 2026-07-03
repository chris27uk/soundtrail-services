using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted;

public sealed class CatalogItemLookupAttemptedHandler(
    MusicCatalogLookupAttemptedHandler musicCatalogLookupAttemptedHandler,
    ApplyCatalogItemLookupAttemptedToArtistDiscovery applyArtistDiscovery,
    ApplyCatalogItemLookupAttemptedToAlbumDiscovery applyAlbumDiscovery) : IHandler<CatalogItemLookupAttempted>
{
    public Task Handle(CatalogItemLookupAttempted attempted, CancellationToken cancellationToken = default)
    {
        return attempted.ItemId switch
        {
            CatalogItemId.Track(var trackId) => musicCatalogLookupAttemptedHandler.Handle(
                new MusicCatalogLookupAttempted(
                    attempted.CommandId,
                    MusicCatalogId.From(trackId.Value),
                    attempted.SourceProvider,
                    attempted.Priority,
                    attempted.CreatedAt,
                    attempted.CorrelationId,
                    attempted.Outcome,
                    attempted.Content is CatalogItemLookupContent.Track(var fetched) ? fetched : null,
                    attempted.SearchCriteria),
                cancellationToken),
            CatalogItemId.Artist(var artistId) => applyArtistDiscovery.Handle(
                artistId,
                attempted.SourceProvider,
                attempted.Priority,
                attempted.CreatedAt,
                attempted.Outcome,
                attempted.Content is CatalogItemLookupContent.Artist(var fetched) ? fetched : null,
                cancellationToken),
            CatalogItemId.Album(var albumId) => applyAlbumDiscovery.Handle(
                albumId.ArtistId,
                albumId.AlbumId,
                attempted.SourceProvider,
                attempted.Priority,
                attempted.CreatedAt,
                attempted.Outcome,
                attempted.Content is CatalogItemLookupContent.Album(var fetched) ? fetched : null,
                cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported catalog item lookup attempted type '{attempted.ItemId.GetType().Name}'.")
        };
    }
}

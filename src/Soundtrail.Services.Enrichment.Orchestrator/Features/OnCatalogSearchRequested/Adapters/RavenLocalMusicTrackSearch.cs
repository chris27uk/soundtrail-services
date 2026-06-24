using Raven.Client.Documents;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Domain.Discovery;
using Soundtrail.Contracts.Persistence;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;

public sealed class RavenLocalMusicTrackSearch(IDocumentStore documentStore) : ILocalMusicTrackSearch
{
    public async Task<LocalMusicTrackSearchResult?> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var document = await session.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        if (document is null)
        {
            return null;
        }

        return new LocalMusicTrackSearchResult(
            musicCatalogId,
            document.CanonicalMetadata?.Title ?? document.Title,
            document.CanonicalMetadata?.Artist ?? document.Artist,
            document.AlbumTitle,
            document.CanonicalMetadata?.Isrc ?? document.Isrc,
            document.CanonicalMetadata?.Mbid ?? document.Mbid,
            document.CanonicalMetadata?.DurationMs ?? document.DurationMs,
            document.IsPlayable,
            string.IsNullOrWhiteSpace(document.ArtistId) ? null : ArtistId.From(document.ArtistId),
            string.IsNullOrWhiteSpace(document.AlbumId) ? null : AlbumId.From(document.AlbumId),
            document.ReleaseDate);
    }
}

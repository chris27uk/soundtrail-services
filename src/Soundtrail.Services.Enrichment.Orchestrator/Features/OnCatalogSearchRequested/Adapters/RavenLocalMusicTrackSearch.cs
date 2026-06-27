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
            document.ResolvedMetadata?.Title ?? document.Title,
            document.ResolvedMetadata?.Artist ?? document.Artist,
            document.AlbumTitle,
            document.ResolvedMetadata?.Isrc ?? document.Isrc,
            document.ResolvedMetadata?.Mbid ?? document.Mbid,
            document.ResolvedMetadata?.DurationMs ?? document.DurationMs,
            document.IsPlayable,
            ToAvailableProviders(document),
            string.IsNullOrWhiteSpace(document.ArtistId) ? null : ArtistId.From(document.ArtistId),
            string.IsNullOrWhiteSpace(document.AlbumId) ? null : AlbumId.From(document.AlbumId),
            document.ReleaseDate);
    }

    private static IReadOnlyList<ProviderName> ToAvailableProviders(RavenTrackRecordDto document)
    {
        var providers = new List<ProviderName>();

        if (!string.IsNullOrWhiteSpace(document.SpotifyId))
        {
            providers.Add(ProviderName.Spotify);
        }

        if (document.AppleReference is not null)
        {
            providers.Add(ProviderName.AppleMusic);
        }

        if (document.YouTubeMusicReference is not null)
        {
            providers.Add(ProviderName.YoutubeMusic);
        }

        return providers;
    }
}

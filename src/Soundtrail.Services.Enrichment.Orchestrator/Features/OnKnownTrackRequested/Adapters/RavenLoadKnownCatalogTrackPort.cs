using Raven.Client.Documents;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.Ports;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.Adapters;

public sealed class RavenLoadKnownCatalogTrackPort(IDocumentStore documentStore) : ILoadKnownCatalogTrackPort
{
    public async Task<LocalMusicTrackSearchResult?> LoadAsync(TrackId trackId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var document = await session.LoadAsync<CatalogTrackDocument>($"catalog/tracks/{trackId.Value}", cancellationToken);
        if (document is null)
        {
            return null;
        }

        return new LocalMusicTrackSearchResult(
            Contracts.Common.MusicCatalogId.From(trackId.Value),
            document.Title,
            document.Artist,
            document.AlbumTitle,
            document.Isrc,
            document.Mbid,
            document.DurationMs,
            document.IsPlayable,
            ToAvailableProviders(document),
            string.IsNullOrWhiteSpace(document.ArtistId) ? null : ArtistId.From(document.ArtistId),
            string.IsNullOrWhiteSpace(document.AlbumId) ? null : AlbumId.From(document.AlbumId),
            document.ReleaseDate);
    }

    private static IReadOnlyList<ProviderName> ToAvailableProviders(CatalogTrackDocument document)
    {
        var providers = new List<ProviderName>();
        if (!string.IsNullOrWhiteSpace(document.SpotifyId))
        {
            providers.Add(ProviderName.Spotify);
        }

        if (!string.IsNullOrWhiteSpace(document.AppleMusicId))
        {
            providers.Add(ProviderName.AppleMusic);
        }

        if (!string.IsNullOrWhiteSpace(document.YouTubeMusicId))
        {
            providers.Add(ProviderName.YoutubeMusic);
        }

        return providers;
    }

    private sealed class CatalogTrackDocument
    {
        public string? Title { get; init; }
        public string? Artist { get; init; }
        public string? AlbumTitle { get; init; }
        public string? Isrc { get; init; }
        public string? Mbid { get; init; }
        public int? DurationMs { get; init; }
        public bool IsPlayable { get; init; }
        public string? ArtistId { get; init; }
        public string? AlbumId { get; init; }
        public string? SpotifyId { get; init; }
        public string? AppleMusicId { get; init; }
        public string? YouTubeMusicId { get; init; }
        public DateOnly? ReleaseDate { get; init; }
    }
}

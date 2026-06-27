using Raven.Client.Documents;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Ports;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Adapters;

public sealed class RavenLoadKnownCatalogTrackPort(IDocumentStore documentStore) : ILoadKnownCatalogTrackPort
{
    public async Task<LocalMusicTrackSearchResult?> LoadAsync(TrackId trackId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var track = await session.LoadAsync<CatalogTrackDocument>(CatalogTrackDocument.GetDocumentId(trackId.Value), cancellationToken);
        if (track is null)
        {
            return null;
        }

        return new LocalMusicTrackSearchResult(
            MusicCatalogId.From(track.TrackId),
            track.Title,
            track.ArtistName,
            track.AlbumName,
            track.Isrc,
            track.MusicBrainzRecordingId,
            track.DurationMs,
            IsPlayable: track.AvailableProviders.Length > 0,
            track.AvailableProviders.Select(ProviderName.From).ToArray(),
            string.IsNullOrWhiteSpace(track.ArtistId) ? null : ArtistId.From(track.ArtistId),
            string.IsNullOrWhiteSpace(track.AlbumId) ? null : AlbumId.From(track.AlbumId));
    }

    private sealed class CatalogTrackDocument
    {
        public string TrackId { get; set; } = string.Empty;

        public string ArtistId { get; set; } = string.Empty;

        public string AlbumId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string AlbumName { get; set; } = string.Empty;

        public string? MusicBrainzRecordingId { get; set; }

        public string? Isrc { get; set; }

        public int? DurationMs { get; set; }

        public string[] AvailableProviders { get; set; } = [];

        public static string GetDocumentId(string trackId) => $"catalog/tracks/{trackId}";
    }
}

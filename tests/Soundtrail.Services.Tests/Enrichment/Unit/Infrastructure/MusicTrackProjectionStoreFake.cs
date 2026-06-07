using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

public sealed class MusicTrackProjectionStoreFake : IMusicTrackProjectionStore
{
    private readonly Dictionary<string, ProjectedMusicTrack> projections = [];

    public IReadOnlyDictionary<string, ProjectedMusicTrack> Projections => projections;

    public Task StoreAsync(
        MusicCatalogId musicCatalogId,
        MusicTrack musicTrack,
        CancellationToken cancellationToken)
    {
        var projection = new ProjectedMusicTrack();
        musicTrack.Project(projection);
        projections[musicCatalogId.Value] = projection;
        return Task.CompletedTask;
    }

    public sealed class ProjectedMusicTrack : IMusicTrackProjectionWriter
    {
        public ProjectedSongMetadata? CanonicalMetadata { get; private set; }

        public ProviderReference? Apple { get; private set; }

        public ProviderReference? YouTubeMusic { get; private set; }

        public bool IsPlayable { get; private set; }

        public void WriteCanonicalMetadata(
            string title,
            string artist,
            string? isrc,
            string? mbid,
            int? durationMs)
        {
            CanonicalMetadata = new ProjectedSongMetadata(title, artist, isrc, mbid, durationMs);
        }

        public void WriteProviderReference(
            ProviderName provider,
            Uri url,
            string? externalId,
            ReferenceConfidence confidence,
            ProviderName sourceProvider)
        {
            var reference = new ProviderReference(provider, url, externalId, confidence, sourceProvider);

            switch (provider)
            {
                case var value when value == ProviderName.AppleMusic:
                    Apple = reference;
                    break;
                case var value when value == ProviderName.YoutubeMusic:
                    YouTubeMusic = reference;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }

        public void WritePlayable(bool isPlayable)
        {
            IsPlayable = isPlayable;
        }
    }

    public sealed record ProjectedSongMetadata(
        string Title,
        string Artist,
        string? Isrc,
        string? Mbid,
        int? DurationMs);
}

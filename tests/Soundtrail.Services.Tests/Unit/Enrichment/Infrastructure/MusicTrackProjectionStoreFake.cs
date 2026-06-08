using System.Collections.Concurrent;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class MusicTrackProjectionStoreFake : IMusicTrackProjectionStore
{
    private readonly ConcurrentDictionary<string, ProjectedMusicTrack> projections = [];

    public IReadOnlyDictionary<string, ProjectedMusicTrack> Projections => projections;

    public Task StoreAsync(
        MusicCatalogId musicCatalogId,
        MusicTrackStream stream,
        CancellationToken cancellationToken)
    {
        var projection = new ProjectedMusicTrack();
        projection.Apply(stream);
        projections[musicCatalogId.Value] = projection;
        return Task.CompletedTask;
    }

    public sealed class ProjectedMusicTrack
    {
        public ProjectedSongMetadata? CanonicalMetadata { get; private set; }

        public ProviderReference? Apple { get; private set; }

        public ProviderReference? YouTubeMusic { get; private set; }

        public bool IsPlayable { get; private set; }

        public void Apply(MusicTrackStream stream)
        {
            ProjectedSongMetadata? canonicalMetadata = null;
            ProviderReference? apple = null;
            ProviderReference? youTubeMusic = null;

            foreach (var fact in stream.Facts)
            {
                switch (fact)
                {
                    case MinimalTrackInfoDiscovered minimalTrackInfoDiscovered:
                        canonicalMetadata = new ProjectedSongMetadata(
                            minimalTrackInfoDiscovered.Title,
                            minimalTrackInfoDiscovered.Artist,
                            minimalTrackInfoDiscovered.Isrc,
                            minimalTrackInfoDiscovered.Mbid,
                            minimalTrackInfoDiscovered.DurationMs);
                        break;
                    case ProviderPlaybackReferenceResolved providerPlaybackReferenceResolved:
                        var reference = new ProviderReference(
                            providerPlaybackReferenceResolved.Provider,
                            providerPlaybackReferenceResolved.Url,
                            providerPlaybackReferenceResolved.ExternalId,
                            ReferenceConfidence.Verified,
                            providerPlaybackReferenceResolved.SourceProvider);

                        switch (providerPlaybackReferenceResolved.Provider)
                        {
                            case var value when value == ProviderName.AppleMusic:
                                apple = reference;
                                break;
                            case var value when value == ProviderName.YoutubeMusic:
                                youTubeMusic = reference;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(
                                    nameof(providerPlaybackReferenceResolved.Provider),
                                    providerPlaybackReferenceResolved.Provider,
                                    null);
                        }

                        break;
                }
            }

            CanonicalMetadata = canonicalMetadata;
            Apple = apple;
            YouTubeMusic = youTubeMusic;
            IsPlayable = canonicalMetadata is not null && (apple is not null || youTubeMusic is not null);
        }
    }

    public sealed record ProjectedSongMetadata(
        string Title,
        string Artist,
        string? Isrc,
        string? Mbid,
        int? DurationMs);
}

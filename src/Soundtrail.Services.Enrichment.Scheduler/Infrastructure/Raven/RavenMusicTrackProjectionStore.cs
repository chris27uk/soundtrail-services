using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven;

public sealed class RavenMusicTrackProjectionStore(
    IAsyncDocumentSession session) : IMusicTrackProjectionStore
{
    public async Task StoreAsync(
        MusicCatalogId musicCatalogId,
        MusicTrack musicTrack,
        CancellationToken cancellationToken)
    {
        var documentId = RavenTrackDocument.GetDocumentId(musicCatalogId.Value);
        var document = await session.LoadAsync<RavenTrackDocument>(documentId, cancellationToken)
            ?? new RavenTrackDocument { Id = documentId };

        musicTrack.Project(new RavenTrackProjectionWriter(document));
        await session.StoreAsync(document, cancellationToken);
    }

    private sealed class RavenTrackProjectionWriter(
        RavenTrackDocument document) : IMusicTrackProjectionWriter
    {
        public void WriteCanonicalMetadata(
            string title,
            string artist,
            string? isrc,
            string? mbid,
            int? durationMs)
        {
            document.CanonicalMetadata = new RavenSongMetadataDocument
            {
                Title = title,
                Artist = artist,
                Isrc = isrc,
                Mbid = mbid,
                DurationMs = durationMs
            };

            document.Title = title;
            document.Artist = artist;
            document.Isrc = isrc;
            document.Mbid = mbid;
            document.DurationMs = durationMs;
            document.SearchText = RavenTrackDocument.BuildSearchText(title, artist);
        }

        public void WriteProviderReference(
            ProviderName provider,
            Uri url,
            string? externalId,
            ReferenceConfidence confidence,
            ProviderName sourceProvider)
        {
            var reference = new RavenProviderReferenceDocument
            {
                Provider = provider.ToString(),
                Url = url.ToString(),
                ExternalId = externalId,
                Confidence = confidence.ToString(),
                SourceProvider = sourceProvider.ToString()
            };

            switch (provider)
            {
                case ProviderName.Apple:
                    document.AppleReference = reference;
                    document.AppleId = externalId;
                    break;
                case ProviderName.YouTubeMusic:
                    document.YouTubeMusicReference = reference;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }

        public void WritePlayable(bool isPlayable)
        {
            document.IsPlayable = isPlayable;
        }
    }
}

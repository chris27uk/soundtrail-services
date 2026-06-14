using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters.Documents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

public sealed class RavenMusicTrackProjectionStore(
    IAsyncDocumentSession session) : IMusicTrackProjectionStore
{
    public async Task StoreAsync(
        MusicCatalogId musicCatalogId,
        MusicTrackStream stream,
        CancellationToken cancellationToken)
    {
        var documentId = RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value);
        var document = await session.LoadAsync<RavenTrackRecordDto>(documentId, cancellationToken)
            ?? new RavenTrackRecordDto { Id = documentId };

        Apply(stream, document);
        await session.StoreAsync(document, cancellationToken);
    }

    private static void Apply(
        MusicTrackStream stream,
        RavenTrackRecordDto document)
    {
        string? title = null;
        string? artist = null;
        string? albumTitle = null;
        string? isrc = null;
        string? mbid = null;
        int? durationMs = null;
        ProviderReference? apple = null;
        ProviderReference? youTubeMusic = null;
        ProviderReference? spotify = null;

        foreach (var fact in stream.Events)
        {
            switch (fact)
            {
                case MinimalTrackInfoDiscovered minimalTrackInfoDiscovered:
                    title = minimalTrackInfoDiscovered.Title;
                    artist = minimalTrackInfoDiscovered.Artist;
                    isrc = minimalTrackInfoDiscovered.Isrc;
                    mbid = minimalTrackInfoDiscovered.Mbid;
                    durationMs = minimalTrackInfoDiscovered.DurationMs;
                    break;
                case ProviderPlaybackReferenceResolved providerPlaybackReferenceResolved:
                    var reference = new ProviderReference(
                        providerPlaybackReferenceResolved.Provider,
                        providerPlaybackReferenceResolved.Url,
                        providerPlaybackReferenceResolved.ExternalId,
                        providerPlaybackReferenceResolved.SourceProvider);

                    switch (providerPlaybackReferenceResolved.Provider)
                    {
                        case var provider when provider == ProviderName.AppleMusic:
                            apple = reference;
                            break;
                        case var provider when provider == ProviderName.YoutubeMusic:
                            youTubeMusic = reference;
                            break;
                        case var provider when provider == ProviderName.Spotify:
                            spotify = reference;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(providerPlaybackReferenceResolved.Provider),
                                providerPlaybackReferenceResolved.Provider,
                                null);
                    }

                    break;
                case TrackLinkedToAlbum trackLinkedToAlbum:
                    albumTitle = trackLinkedToAlbum.AlbumTitle;
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist))
        {
            document.CanonicalMetadata = new RavenSongMetadataRecordDto
            {
                Title = title,
                Artist = artist,
                Isrc = isrc,
                Mbid = mbid,
                DurationMs = durationMs
            };

            document.Title = title;
            document.Artist = artist;
            document.AlbumTitle = albumTitle;
            document.Isrc = isrc;
            document.Mbid = mbid;
            document.DurationMs = durationMs;
            document.SearchText = RavenTrackRecordDto.BuildSearchText(title, artist);
        }

        if (apple is not null)
        {
            document.AppleReference = new RavenProviderReferenceRecordDto
            {
                Provider = apple.Provider.ToString(),
                Url = apple.Url.ToString(),
                ExternalId = apple.ExternalId,
                SourceProvider = apple.SourceProvider.ToString()
            };
            document.AppleId = apple.ExternalId;
        }

        if (youTubeMusic is not null)
        {
            document.YouTubeMusicReference = new RavenProviderReferenceRecordDto
            {
                Provider = youTubeMusic.Provider.ToString(),
                Url = youTubeMusic.Url.ToString(),
                ExternalId = youTubeMusic.ExternalId,
                SourceProvider = youTubeMusic.SourceProvider.ToString()
            };
        }

        if (spotify is not null)
        {
            document.SpotifyId = spotify.ExternalId;
        }

        document.IsPlayable =
            !string.IsNullOrWhiteSpace(title)
            && !string.IsNullOrWhiteSpace(artist)
            && (apple is not null || youTubeMusic is not null || spotify is not null);
    }
}

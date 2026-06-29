using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Support;

public abstract class KnownTrackRequestedMusicTrack
{
    public static KnownTrackRequestedMusicTrack Missing { get; } = new MissingKnownTrackRequestedMusicTrack();

    public abstract LookupStreamingLocationsCommand? CreateLookupCommand(
        KnownTrackRequested requested);

    public static KnownTrackRequestedMusicTrack Available(
        MusicCatalogId musicCatalogId,
        string? title,
        string? artist,
        string? albumTitle,
        string? isrc,
        IReadOnlyList<Contracts.Common.ProviderName> availableProviders,
        ArtistId? artistId,
        AlbumId? albumId) =>
        new AvailableKnownTrackRequestedMusicTrack(
            musicCatalogId,
            title,
            artist,
            albumTitle,
            isrc,
            availableProviders,
            artistId,
            albumId);

    private sealed class MissingKnownTrackRequestedMusicTrack : KnownTrackRequestedMusicTrack
    {
        public override LookupStreamingLocationsCommand? CreateLookupCommand(KnownTrackRequested requested)
        {
            _ = requested;
            return null;
        }
    }

    private sealed class AvailableKnownTrackRequestedMusicTrack(
        MusicCatalogId musicCatalogId,
        string? title,
        string? artist,
        string? albumTitle,
        string? isrc,
        IReadOnlyList<Contracts.Common.ProviderName> availableProviders,
        ArtistId? artistId,
        AlbumId? albumId) : KnownTrackRequestedMusicTrack
    {
        public override LookupStreamingLocationsCommand? CreateLookupCommand(KnownTrackRequested requested)
        {
            if (!CanCreateSearchTerm(isrc, title, artist)
                || !requested.Playback.RequiresAnyMissing(availableProviders))
            {
                return null;
            }

            return new LookupStreamingLocationsCommand(
                LookupStreamingLocationsCommand.Id(musicCatalogId),
                musicCatalogId,
                Contracts.Common.LookupPriorityBand.Low,
                requested.RequestedAt,
                requested.CorrelationId,
                BuildSearchCriteria(isrc, title, artist, albumTitle),
                artistId is null && albumId is null
                    ? null
                    : new CatalogTrackHierarchy(artistId, albumId));
        }

        private static bool CanCreateSearchTerm(string? currentIsrc, string? currentTitle, string? currentArtist) =>
            !string.IsNullOrWhiteSpace(currentIsrc)
            || (!string.IsNullOrWhiteSpace(currentTitle) && !string.IsNullOrWhiteSpace(currentArtist));

        private static Domain.Search.MusicSearchCriteria BuildSearchCriteria(
            string? currentIsrc,
            string? currentTitle,
            string? currentArtist,
            string? currentAlbumTitle)
        {
            if (!string.IsNullOrWhiteSpace(currentIsrc))
            {
                return Domain.Search.MusicSearchCriteria.ByIsrc(currentIsrc);
            }

            return Domain.Search.MusicSearchCriteria.ByTrackArtistAlbum(
                currentTitle!,
                currentArtist!,
                currentAlbumTitle);
        }
    }
}

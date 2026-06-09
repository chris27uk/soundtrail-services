using Soundtrail.Contracts.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

internal static class LookupCommandMappings
{
    public static object ToMessage(this LookupPhaseCommand command) =>
        command switch
        {
            LookupCanonicalMusicMetadataCommand musicBrainz => new LookupCanonicalMusicMetadataCommandDto(
                musicBrainz.CommandId.Value,
                musicBrainz.MusicCatalogId.Value,
                musicBrainz.Priority,
                musicBrainz.CreatedAt,
                musicBrainz.CorrelationId.Value,
                musicBrainz.Lookup switch
                {
                    CanonicalMusicMetadataLookup.ByIsrc byIsrc => byIsrc.Isrc,
                    _ => null
                },
                musicBrainz.Lookup switch
                {
                    CanonicalMusicMetadataLookup.ByTrackNameArtistAndAlbum byTrack => byTrack.TrackName,
                    _ => null
                },
                musicBrainz.Lookup switch
                {
                    CanonicalMusicMetadataLookup.ByTrackNameArtistAndAlbum byTrack => byTrack.ArtistName,
                    _ => null
                },
                musicBrainz.Lookup switch
                {
                    CanonicalMusicMetadataLookup.ByTrackNameArtistAndAlbum byTrack => byTrack.AlbumName,
                    _ => null
                }),
            ResolvePlaybackReferencesCommand playback => new ResolvePlaybackReferencesCommandDto(
                playback.CommandId.Value,
                playback.MusicCatalogId.Value,
                playback.Priority,
                playback.CreatedAt,
                playback.CorrelationId.Value,
                new PlaybackReferenceLookupKeyDto(
                    (PlaybackReferenceLookupModeDto)playback.LookupKey.Mode,
                    playback.LookupKey.Isrc,
                    playback.LookupKey.Title,
                    playback.LookupKey.Artist)),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
}

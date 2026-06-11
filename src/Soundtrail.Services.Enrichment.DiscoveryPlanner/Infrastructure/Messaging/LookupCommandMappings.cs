using Soundtrail.Contracts.Commands;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

internal static class LookupCommandMappings
{
    public static object ToMessage(this object command) =>
        command switch
        {
            LookupCanonicalMusicMetadataCommand musicBrainz => new LookupCanonicalMusicMetadataCommandDto(
                musicBrainz.CommandId.Value,
                musicBrainz.MusicCatalogId.Value,
                musicBrainz.Priority,
                musicBrainz.CreatedAt,
                musicBrainz.CorrelationId.Value,
                musicBrainz.SearchTerm.Isrc,
                musicBrainz.SearchTerm.Title,
                musicBrainz.SearchTerm.Artist,
                musicBrainz.SearchTerm.Album
            ),
            ResolvePlaybackReferencesCommand playback => new ResolvePlaybackReferencesCommandDto(
                playback.CommandId.Value,
                playback.MusicCatalogId.Value,
                playback.Priority,
                playback.CreatedAt,
                playback.CorrelationId.Value,
                new PlaybackReferenceSearchTermDto(
                    playback.SearchTerm.Isrc,
                    playback.SearchTerm.Title,
                    playback.SearchTerm.Artist,
                    playback.SearchTerm.Album)),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
}

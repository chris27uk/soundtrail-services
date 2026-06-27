using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;

internal static class LookupCommandMappings
{
    public static object ToMessage(this object command) =>
        command switch
        {
            LookupMusicMetadataCommand musicBrainz => new LookupMusicMetadataCommandDto(
                musicBrainz.CommandId.Value,
                musicBrainz.MusicCatalogId.Value,
                musicBrainz.Priority,
                musicBrainz.CreatedAt,
                musicBrainz.CorrelationId.Value,
                musicBrainz.SearchCriteria.Kind,
                musicBrainz.SearchCriteria.Query,
                musicBrainz.SearchCriteria.Isrc,
                musicBrainz.SearchCriteria.Title,
                musicBrainz.SearchCriteria.Artist,
                musicBrainz.SearchCriteria.Album,
                musicBrainz.Hierarchy?.ArtistId?.Value,
                musicBrainz.Hierarchy?.AlbumId?.Value
            ),
            LookupStreamingLocationsCommand playback => new LookupStreamingLocationsCommandDto(
                playback.CommandId.Value,
                playback.MusicCatalogId.Value,
                playback.Priority,
                playback.CreatedAt,
                playback.CorrelationId.Value,
                new StreamingLocationSearchTermDto(
                    playback.LookupKey.Kind,
                    playback.LookupKey.Query,
                    playback.LookupKey.Isrc,
                    playback.LookupKey.Title,
                    playback.LookupKey.Artist,
                    playback.LookupKey.Album),
                playback.Hierarchy?.ArtistId?.Value,
                playback.Hierarchy?.AlbumId?.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
}

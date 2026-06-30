using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Lookup;

internal static class LookupTrackMetadataCommandExtensions
{
    public static MusicCatalogMetadataFetched ToMusicCatalogMetadataFetched(
        this LookupTrackMetadataCommand command,
        SongMetadata? metadata) =>
        new(
            command.CommandId,
            command.MusicCatalogId,
            LookupSource.MusicBrainz,
            command.Priority,
            command.CreatedAt,
            metadata,
            [],
            [],
            command.Hierarchy,
            command.CorrelationId);
}

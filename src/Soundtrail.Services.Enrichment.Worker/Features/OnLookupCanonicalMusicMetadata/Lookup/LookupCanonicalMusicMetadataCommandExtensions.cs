using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupCanonicalMusicMetadata.Lookup;

internal static class LookupCanonicalMusicMetadataCommandExtensions
{
    public static MusicCatalogMetadataFetched ToMusicCatalogMetadataFetched(
        this LookupMusicMetadataCommand command,
        SongMetadata? metadata) =>
        new(
            command.CommandId,
            command.MusicCatalogId,
            ProviderName.MusicBrainz,
            command.Priority,
            command.CreatedAt,
            metadata,
            [],
            [],
            command.Hierarchy,
            command.CorrelationId);
}

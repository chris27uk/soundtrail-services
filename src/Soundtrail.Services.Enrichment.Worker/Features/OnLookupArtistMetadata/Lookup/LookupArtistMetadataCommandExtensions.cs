using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Lookup;

internal static class LookupArtistMetadataCommandExtensions
{
    public static ArtistMetadataFetched ToArtistMetadataFetched(
        this LookupArtistMetadataCommand command,
        ArtistMetadata metadata) =>
        new(
            command.CommandId,
            command.ArtistId,
            LookupSource.MusicBrainz,
            command.Priority,
            command.CreatedAt,
            metadata,
            command.CorrelationId);
}

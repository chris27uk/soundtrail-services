using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Lookup;

internal static class LookupAlbumMetadataCommandExtensions
{
    public static AlbumMetadataFetched ToAlbumMetadataFetched(
        this LookupAlbumMetadataCommand command,
        AlbumMetadata metadata) =>
        new(
            command.CommandId,
            command.ArtistId,
            command.AlbumId,
            LookupSource.MusicBrainz,
            command.Priority,
            command.CreatedAt,
            metadata,
            command.CorrelationId);
}

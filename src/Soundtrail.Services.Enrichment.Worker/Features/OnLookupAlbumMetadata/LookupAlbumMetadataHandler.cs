using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Lookup;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata;

public sealed class LookupAlbumMetadataHandler(IGetAlbumMetadata getAlbumMetadata, ICommandBus bus) : IHandler<LookupAlbumMetadataCommand>
{
    public async Task Handle(LookupAlbumMetadataCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await getAlbumMetadata.GetMetadataAsync(
                command.ArtistName,
                command.AlbumTitle,
                command.SourceAlbumId,
                command.SourceArtistId,
                cancellationToken);

            if (metadata is null)
            {
                await bus.SendAsync(
                    CatalogItemLookupAttempted.Failed(
                        command.CommandId,
                        command.ArtistId,
                        command.AlbumId,
                        LookupSource.MusicBrainz,
                        command.Priority,
                        command.CreatedAt,
                        command.CorrelationId,
                        "Album lookup returned no metadata"),
                    cancellationToken);
                return;
            }

            await bus.SendAsync(
                CatalogItemLookupAttempted.Completed(command.ToAlbumMetadataFetched(metadata)),
                cancellationToken);
        }
        catch
        {
            await bus.SendAsync(
                CatalogItemLookupAttempted.Failed(
                    command.CommandId,
                    command.ArtistId,
                    command.AlbumId,
                    LookupSource.MusicBrainz,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId,
                    "Lookup failed"),
                cancellationToken);
        }
    }
}

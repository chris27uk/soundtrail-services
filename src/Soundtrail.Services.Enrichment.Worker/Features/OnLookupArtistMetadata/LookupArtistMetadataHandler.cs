using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Lookup;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata;

public sealed class LookupArtistMetadataHandler(IGetArtistMetadata getArtistMetadata, ICommandBus bus) : IHandler<LookupArtistMetadataCommand>
{
    public async Task Handle(LookupArtistMetadataCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await getArtistMetadata.GetMetadataAsync(
                command.ArtistName,
                command.SourceArtistId,
                cancellationToken);

            if (metadata is null)
            {
                await bus.SendAsync(
                    ArtistMetadataLookupAttempted.Failed(
                        command.CommandId,
                        command.ArtistId,
                        LookupSource.MusicBrainz,
                        command.Priority,
                        command.CreatedAt,
                        command.CorrelationId,
                        "Artist lookup returned no metadata"),
                    cancellationToken);
                return;
            }

            await bus.SendAsync(
                ArtistMetadataLookupAttempted.Completed(command.ToArtistMetadataFetched(metadata)),
                cancellationToken);
        }
        catch
        {
            await bus.SendAsync(
                ArtistMetadataLookupAttempted.Failed(
                    command.CommandId,
                    command.ArtistId,
                    LookupSource.MusicBrainz,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId,
                    "Lookup failed"),
                cancellationToken);
        }
    }
}

using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Lookup;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata;

public sealed class LookupTrackMetadataHandler(IGetTrackMetadata getMetaData, ICommandBus bus) : IHandler<LookupTrackMetadataCommand>
{
    public async Task Handle(LookupTrackMetadataCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var songMetadata = await getMetaData.GetMetadataAsync(command.SearchCriteria, cancellationToken);
            await bus.SendAsync(
                MusicCatalogLookupAttempted.Completed(
                    command.ToMusicCatalogMetadataFetched(songMetadata),
                    command.SearchCriteria),
                cancellationToken);
        }
        catch
        {
            await bus.SendAsync(MusicCatalogLookupAttempted.Failed(
                command.CommandId,
                command.MusicCatalogId,
                LookupSource.MusicBrainz,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId,
                "Lookup failed",
                command.SearchCriteria), cancellationToken);
        }
    }
}

using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Lookup;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata;

public sealed class LookupTrackMetadataHandler(IGetTrackMetadata getMetaData, ICommandBus bus) : IHandler<LookupTrackCommand>
{
    public async Task Handle(LookupTrackCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var songMetadata = await getMetaData.GetMetadataAsync(command.SearchCriteria, cancellationToken);
            await bus.SendAsync(
                CatalogItemLookupAttempted.Completed(
                    command.ToMusicCatalogMetadataFetched(songMetadata),
                    command.SearchCriteria),
                cancellationToken);
        }
        catch
        {
            await bus.SendAsync(CatalogItemLookupAttempted.Failed(
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

public class LookupTrackCommand(LookupCriteria searchCriteria)
{
    public LookupCriteria SearchCriteria { get;} = searchCriteria;
}

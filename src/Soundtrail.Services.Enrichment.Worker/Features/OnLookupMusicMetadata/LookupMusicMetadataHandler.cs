using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Lookup;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata;

public sealed class LookupMusicMetadataHandler(
    IGetMusicMetadata getMetaData) : ILookupMusicMetadataHandler
{
    public async Task<MusicCatalogLookupAttempted> Handle(
        LookupMusicMetadataCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var songMetadata = await getMetaData.GetMetadataAsync(command.SearchTerm, cancellationToken);
            return MusicCatalogLookupAttempted.Completed(command.ToMusicCatalogMetadataFetched(songMetadata));
        }
        catch
        {
            return MusicCatalogLookupAttempted.Failed(
                command.CommandId,
                command.MusicCatalogId,
                ProviderName.MusicBrainz,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId,
                "Lookup failed");
        }
    }
}

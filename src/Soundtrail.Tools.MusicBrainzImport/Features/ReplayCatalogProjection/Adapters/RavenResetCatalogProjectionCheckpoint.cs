using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.Adapters;

public sealed class RavenResetCatalogProjectionCheckpoint(
    IAsyncDocumentSession session) : IResetCatalogProjectionCheckpointPort
{
    public async Task ResetAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var changed = false;

        var trackDocument = await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);
        if (trackDocument is not null)
        {
            session.Delete(trackDocument);
            changed = true;
        }

        var checkpoint = await session.LoadAsync<CatalogProjectionCheckpointDocument>(
            CatalogProjectionCheckpointDocument.GetDocumentId(musicCatalogId.Value),
            cancellationToken);
        if (checkpoint is not null)
        {
            session.Delete(checkpoint);
            changed = true;
        }

        if (changed)
        {
            await session.SaveChangesAsync(cancellationToken);
            session.Advanced.Clear();
        }
    }
}

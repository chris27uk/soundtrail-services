using Raven.Client.Documents.Session;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.ProjectionReset;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.Adapters;

public sealed class RavenResetCatalogProjectionCheckpoint(
    IAsyncDocumentSession session) : IResetCatalogProjectionCheckpointPort
{
    public async Task ResetAsync(
        ArtistId artistId,
        CancellationToken cancellationToken)
    {
        var changed = false;

        var checkpoint = await session.LoadAsync<CatalogProjectionCheckpointDocument>(
            CatalogProjectionCheckpointDocument.GetDocumentId(artistId.Value),
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

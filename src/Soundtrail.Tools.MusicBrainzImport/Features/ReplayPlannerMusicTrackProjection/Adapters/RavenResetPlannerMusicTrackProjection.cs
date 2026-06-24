using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection.Adapters;

public sealed class RavenResetPlannerMusicTrackProjection(
    IAsyncDocumentSession session) : IResetPlannerMusicTrackProjectionPort
{
    public async Task ResetAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var document = await session.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        if (document is null)
        {
            return;
        }

        session.Delete(document);
        await session.SaveChangesAsync(cancellationToken);
        session.Advanced.Clear();
    }
}

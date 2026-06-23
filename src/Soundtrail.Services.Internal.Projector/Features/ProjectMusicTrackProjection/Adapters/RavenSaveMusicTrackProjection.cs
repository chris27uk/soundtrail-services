using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.Ports;
using Soundtrail.Domain.Model;
using Soundtrail.Contracts.Persistence;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.Adapters;

public sealed class RavenSaveMusicTrackProjection(
    IAsyncDocumentSession session,
    RavenMusicTrackProjectionMapper mapper) : ISaveMusicTrackProjectionPort
{
    public async Task SaveAsync(
        MusicCatalogId musicCatalogId,
        MusicTrackProjection projection,
        CancellationToken cancellationToken)
    {
        var documentId = RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value);
        var document = await session.LoadAsync<RavenTrackRecordDto>(documentId, cancellationToken)
            ?? new RavenTrackRecordDto
            {
                Id = documentId
            };

        mapper.MapOntoDocument(document, projection);
        await session.StoreAsync(document, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}

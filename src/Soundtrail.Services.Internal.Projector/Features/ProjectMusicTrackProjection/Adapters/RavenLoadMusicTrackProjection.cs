using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.Ports;
using Soundtrail.Domain.Model;
using Soundtrail.Contracts.Persistence;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.Adapters;

public sealed class RavenLoadMusicTrackProjection(
    IAsyncDocumentSession session,
    RavenMusicTrackProjectionMapper mapper) : ILoadMusicTrackProjectionPort
{
    public async Task<MusicTrackProjection> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var documentId = RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value);
        var document = await session.LoadAsync<RavenTrackRecordDto>(documentId, cancellationToken)
            ?? new RavenTrackRecordDto
            {
                Id = documentId
            };

        return mapper.ToDomain(document);
    }
}

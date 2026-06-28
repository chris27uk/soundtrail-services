using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Ports;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;

public sealed class RavenSaveMusicTrackProjection(
    IAsyncDocumentSession session,
    ITypeRegistry translator) : ISaveMusicTrackProjectionPort
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

        translator.MapOnto(projection, document);
        await session.StoreAsync(document, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}

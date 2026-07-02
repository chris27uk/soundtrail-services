using Raven.Client.Documents.Session;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;

public sealed class RavenSaveMusicTrackCatalogProjection(
    IAsyncDocumentSession session) : ISaveMusicTrackCatalogProjectionPort
{
    public RavenSaveMusicTrackCatalogProjection(
        IAsyncDocumentSession session,
        object ignored)
        : this(session)
    {
        _ = ignored;
    }

    public async Task SaveAsync(
        ArtistCatalogProjection projection,
        CancellationToken cancellationToken)
    {
        if (projection.ArtistDocument is not null)
        {
            await EvictTrackedAsync(projection.ArtistDocument.Id, cancellationToken);
            await session.StoreAsync(projection.ArtistDocument, cancellationToken);
        }

        foreach (var album in projection.AlbumDocuments)
        {
            await EvictTrackedAsync(album.Id, cancellationToken);
            await session.StoreAsync(album, cancellationToken);
        }

        foreach (var track in projection.TrackDocuments)
        {
            await EvictTrackedAsync(track.Id, cancellationToken);
            await session.StoreAsync(track, cancellationToken);
        }

        foreach (var trackProjection in projection.RavenTrackDocuments)
        {
            await EvictTrackedAsync(trackProjection.Id, cancellationToken);
            await session.StoreAsync(trackProjection, cancellationToken);
        }

        await EvictTrackedAsync(projection.CheckpointDocument.Id, cancellationToken);
        await session.StoreAsync(projection.CheckpointDocument, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }

    private async Task EvictTrackedAsync(string id, CancellationToken cancellationToken)
    {
        var tracked = await session.LoadAsync<object>(id, cancellationToken);
        if (tracked is not null)
        {
            session.Advanced.Evict(tracked);
        }
    }
}

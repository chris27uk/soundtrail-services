using Raven.Client.Documents;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForPlaylist.Adapters;

public sealed class RavenGetTracksForPlaylistPort(IDocumentStore documentStore, ITypeRegistry typeRegistry) : IGetTracksForPlaylistPort
{
    public async Task<GetTracksForPlaylistResponse?> GetTracksForPlaylistAsync(PlaylistId playlistId, CancellationToken cancellationToken)
    {
        var activeSession = documentStore.OpenAsyncSession();
        var documentId = CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId.Value);
        var existing = await activeSession.LoadAsync<CatalogPlaylistTracksRecordDto>(documentId, cancellationToken);
        return existing is null ? null : typeRegistry.ToDomainObject<GetTracksForPlaylistResponse>(existing);
    }
}

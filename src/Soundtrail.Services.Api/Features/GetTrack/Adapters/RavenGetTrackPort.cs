using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.GetTrack.Adapters;

public sealed class RavenGetTrackPort(IDocumentStore documentStore, ITypeRegistry typeRegistry) : IGetTrackPort
{
    public async Task<GetTrackResponse?> GetTrackAsync(TrackId trackId, CancellationToken cancellationToken)
    {
        var activeSession = documentStore.OpenAsyncSession();
        var documentId = CatalogTrackRecordDto.GetDocumentId(trackId.Value);
        var existing = await activeSession.LoadAsync<CatalogTrackRecordDto>(documentId, cancellationToken);
        return existing is null ? null : typeRegistry.ToDomainObject<GetTrackResponse>(existing);
    }
}

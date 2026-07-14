using Raven.Client.Documents;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Api.Features.Search.Adapters;

public sealed class RavenSearchPort(IDocumentStore documentStore, ITypeRegistry typeRegistry) : ISearchPort
{
    public async Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken)
    {
        var activeSession = documentStore.OpenAsyncSession();
        var documentId = CatalogSearchRecordDto.GetDocumentId(searchCriteria.NormalisedIdentifier);
        var existing = await activeSession.LoadAsync<CatalogSearchRecordDto>(documentId, cancellationToken);
        return existing is null ? null : typeRegistry.ToDomainObject<SearchResponse>(existing);
    }
}

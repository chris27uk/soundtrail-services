using Raven.Client.Documents;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public sealed class RavenResolutionDemandStore(IDocumentStore documentStore) : IResolutionDemandPort
{
    public async Task<QueryId> RecordDemandAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var documentId = RavenResolutionDemandDocument.GetDocumentId(query);
        var document = await session.LoadAsync<RavenResolutionDemandDocument>(documentId, cancellationToken);

        if (document is null)
        {
            var queryId = QueryId.New();
            document = new RavenResolutionDemandDocument
            {
                Id = documentId,
                Query = query.Value,
                QueryId = queryId.Value,
                Count = 1
            };

            await session.StoreAsync(document, cancellationToken);
            await session.SaveChangesAsync(cancellationToken);
            return queryId;
        }

        document.Count += 1;
        await session.SaveChangesAsync(cancellationToken);
        return RavenMappings.QueryIdFrom(document.QueryId);
    }
}

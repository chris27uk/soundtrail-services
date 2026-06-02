using Raven.Client.Documents;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public sealed class RavenQueryCache(IDocumentStore documentStore) : IQueryCachePort
{
    public async Task<SearchMusicResponse?> GetAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var document = await session.LoadAsync<Documents.RavenQueryCacheDocument>(
            Documents.RavenQueryCacheDocument.GetDocumentId(query),
            cancellationToken);

        return document is null
            ? null
            : document.ToDomain(SearchQuery.From(query.Value));
    }

    public async Task StoreAsync(
        NormalizedSearchQuery query,
        SearchMusicResponse response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(response.ToDocument(query), cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}

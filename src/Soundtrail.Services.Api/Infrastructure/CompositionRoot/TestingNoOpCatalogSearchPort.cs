using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Api.Infrastructure.CompositionRoot;

internal sealed class TestingNoOpCatalogSearchPort : ICatalogSearchPort
{
    public Task<LocalCatalogSearchResponse> SearchAsync(SearchCatalogCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(new LocalCatalogSearchResponse([], null, IsComplete: true));
}
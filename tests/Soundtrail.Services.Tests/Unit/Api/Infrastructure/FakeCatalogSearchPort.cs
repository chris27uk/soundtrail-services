using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;

namespace Soundtrail.Services.Tests.Unit.Api.Infrastructure;

internal sealed class FakeCatalogSearchPort : ICatalogSearchPort
{
    public LocalCatalogSearchResponse Response { get; set; } = new([], null, true);

    public Task<LocalCatalogSearchResponse> SearchAsync(SearchCatalogCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Response);
}

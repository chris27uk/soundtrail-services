using Soundtrail.Services.Features.CatalogLookup.Contracts;

namespace Soundtrail.Services.Tests.Integration.Features.Search
{
    public sealed class ApiFakeCatalogLookupPort : ICatalogLookupPort
    {
        public bool Ready { get; set; } = true;

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(Ready);
    }
}

using Soundtrail.Services.Features.CatalogLookup.Contracts;

namespace Soundtrail.Services.Api.Infrastructure.TableStorage;

public sealed class AzureTableTrackLookup : ICatalogLookupPort
{
    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}

using Soundtrail.Services.Application.Ports;

namespace Soundtrail.Services.Api.Infrastructure.TableStorage;

public sealed class AzureTableTrackLookup : ITrackLookupPort
{
    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}

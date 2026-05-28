using MusicResolver.Application.Ports;

namespace MusicResolver.Infrastructure.TableStorage;

public sealed class AzureTableTrackLookup : ITrackLookupPort
{
    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}

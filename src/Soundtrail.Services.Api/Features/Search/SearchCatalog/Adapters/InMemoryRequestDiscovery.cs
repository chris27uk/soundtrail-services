using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters;

public sealed class InMemoryRequestDiscovery(
    IQueueLookupMusicRequestPort? queueLookupMusicRequestPort = null) : IRequestDiscoveryPort
{
    private readonly HashSet<string> queryKeys = [];

    public List<RequestDiscoveryCommand> Requests { get; } = [];

    public void Seed(DiscoveryQueryKey queryKey) => queryKeys.Add(queryKey.Value);

    public async Task<bool> TryRequestAsync(
        RequestDiscoveryCommand command,
        CancellationToken cancellationToken)
    {
        if (!queryKeys.Add(command.QueryKey.Value))
        {
            return false;
        }

        Requests.Add(command);
        if (queueLookupMusicRequestPort is not null)
        {
            await queueLookupMusicRequestPort.EnqueueAsync(command.Request, cancellationToken);
        }

        return true;
    }
}

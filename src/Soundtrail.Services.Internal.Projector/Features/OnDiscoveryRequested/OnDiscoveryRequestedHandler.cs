using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnDiscoveryRequested.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnDiscoveryRequested;

public sealed class OnDiscoveryRequestedHandler(ICommandBus commandBus)
{
    public async Task Handle(
        DiscoveryRequestedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events.OrderBy(x => x.Version))
        {
            var requested = (DiscoveryRequested)item.Event;
            if (requested.Playback is null)
            {
                continue;
            }

            await commandBus.SendAsync(
                new SearchCatalogRequested(
                    requested.SearchCriteria,
                    requested.Playback,
                    requested.TrustLevel,
                    requested.RiskScore,
                    requested.RequestedAt,
                    requested.CorrelationId)
                {
                    CommandId = CommandId.For(
                        $"SearchCatalogRequested:{command.StreamId.StableValue}:{item.Version}")
                },
                cancellationToken);
        }
    }
}

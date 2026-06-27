using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;

public sealed class CatalogSearchStatusChangedHandler(
    ILoadDiscoveryLifecycleProjectionPort loadPort,
    ISaveDiscoveryLifecycleProjectionPort savePort) : IHandler<CatalogSearchStatusChangedCommand>
{
    public async Task Handle(
        CatalogSearchStatusChangedCommand request,
        CancellationToken cancellationToken = default)
    {
        var projection = await loadPort.LoadAsync(request.SearchCriteria, cancellationToken);

        foreach (var item in request.Events.OrderBy(x => x.Version))
        {
            if (projection.ProjectionVersion >= item.Version)
            {
                continue;
            }

            projection.Apply(item.Event, item.Version);
        }

        await savePort.SaveAsync(projection, cancellationToken);
    }
}

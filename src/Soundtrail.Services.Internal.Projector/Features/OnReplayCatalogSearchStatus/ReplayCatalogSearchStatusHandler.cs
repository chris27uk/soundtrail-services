using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus;

public sealed class ReplayCatalogSearchStatusHandler(
    ILoadStoredDiscoveryLifecycleEventsPort loadPort,
    CatalogSearchStatusChangedHandler projectHandler) : IHandler<ReplayCatalogSearchStatusCommand>
{
    public async Task Handle(
        ReplayCatalogSearchStatusCommand request,
        CancellationToken cancellationToken = default)
    {
        var events = await loadPort.LoadAsync(request.Criteria, cancellationToken);
        await projectHandler.Handle(new CatalogSearchStatusChangedCommand(request.Criteria, events), cancellationToken);
    }
}

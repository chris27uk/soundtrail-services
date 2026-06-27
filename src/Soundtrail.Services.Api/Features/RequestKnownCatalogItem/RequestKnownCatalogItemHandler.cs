using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Api.Features.RequestKnownCatalogItem.Ports;

namespace Soundtrail.Services.Api.Features.RequestKnownCatalogItem;

public sealed class RequestKnownCatalogItemHandler(IQueueKnownCatalogItemRequestPort queuePort)
{
    public Task Handle(KnownCatalogItemRequested request, CancellationToken cancellationToken = default) =>
        queuePort.EnqueueAsync(request, cancellationToken);
}

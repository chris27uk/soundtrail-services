using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Api.Features.RequestKnownCatalogItem.Ports;

public interface IQueueKnownCatalogItemRequestPort
{
    Task EnqueueAsync(KnownCatalogItemRequested request, CancellationToken cancellationToken);
}

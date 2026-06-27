using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Api.Features.RequestKnownCatalogItem.Ports;
using Wolverine;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public sealed class WolverineEnqueueKnownCatalogItemRequest(IMessageBus messageBus) : IEnqueueKnownCatalogItemRequest
{
    public Task EnqueueAsync(KnownCatalogItemRequested request, CancellationToken cancellationToken) =>
        messageBus.SendAsync(KnownCatalogItemRequestedMapper.ToDto(request)).AsTask();
}

using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;
using Wolverine;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public sealed class WolverineEnqueueCatalogSearchAttempt(
    IMessageBus messageBus) : IEnqueueCatalogSearchAttempt
{
    public Task EnqueueAsync(
        SearchCatalogRequested requested,
        CancellationToken cancellationToken) =>
        messageBus.SendAsync(CatalogSearchAttemptMapper.ToDto(requested)).AsTask();
}

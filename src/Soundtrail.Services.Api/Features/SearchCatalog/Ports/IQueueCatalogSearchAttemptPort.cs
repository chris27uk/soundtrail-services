using Soundtrail.Domain.Commands;

namespace Soundtrail.Services.Api.Features.SearchCatalog.Ports;

public interface IQueueCatalogSearchAttemptPort
{
    Task EnqueueAsync(CatalogSearchAttempt request, CancellationToken cancellationToken);
}

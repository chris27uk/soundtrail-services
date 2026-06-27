using Soundtrail.Domain.Commands;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearchAttemptQueue
{
    internal sealed class ThrowingCatalogSearchAttemptQueue : IEnqueueCatalogSearchAttempt
    {
        public Task EnqueueAsync(SearchCatalogRequested requested, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("No route configured."));
    }
}

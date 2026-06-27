using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearchAttemptQueue
{
    internal sealed class ThrowingCatalogSearchAttemptQueue : ICommandBus
    {
        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("No route configured."));
    }
}

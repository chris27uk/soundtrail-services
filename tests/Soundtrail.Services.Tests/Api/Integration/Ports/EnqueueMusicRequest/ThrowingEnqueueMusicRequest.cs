using Soundtrail.Contracts;
using Soundtrail.Services.Api.Features.Search.Queueing;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest
{
    internal sealed class ThrowingEnqueueMusicRequest : IEnqueueMusicRequest
    {
        public Task EnqueueAsync(LookupMusicRequest request, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("No route configured."));
    }
}
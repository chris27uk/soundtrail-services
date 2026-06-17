using Soundtrail.Domain.Commands;
using Soundtrail.Services.Api.Features.SearchMusic.Queueing;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.EnqueueMusicRequest
{
    internal sealed class ThrowingEnqueueMusicRequest : IEnqueueMusicRequest
    {
        public Task EnqueueAsync(LookupMusicRequest request, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("No route configured."));
    }
}

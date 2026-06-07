using Soundtrail.Services.Api.Features.Search.Queueing;

namespace Soundtrail.Services.Tests.Api.Unit.Infrastructure
{
    internal sealed class FakeEnqueueMusicRequest : IEnqueueMusicRequest
    {
        private readonly List<LookupMusicRequest> requests = [];

        public IReadOnlyList<LookupMusicRequest> Requests => this.requests;

        public Task EnqueueAsync(
            LookupMusicRequest request,
            CancellationToken cancellationToken)
        {
            this.requests.Add(request);
            return Task.CompletedTask;
        }
    }
}

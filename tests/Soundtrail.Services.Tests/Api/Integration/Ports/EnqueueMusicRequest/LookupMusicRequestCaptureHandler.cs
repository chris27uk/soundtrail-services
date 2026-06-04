using Soundtrail.Services.Features.Search.Queueing;
using Wolverine.Attributes;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.EnqueueMusicRequest
{
    public sealed class LookupMusicRequestCaptureHandler(LookupMusicRequestCapture capture)
    {
        [WolverineHandler]
        public Task Handle(LookupMusicRequest request, CancellationToken cancellationToken)
        {
            capture.Record(request);
            return Task.CompletedTask;
        }
    }
}
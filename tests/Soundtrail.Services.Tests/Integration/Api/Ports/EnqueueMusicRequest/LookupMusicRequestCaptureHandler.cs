using Soundtrail.Contracts;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Wolverine.Attributes;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.EnqueueMusicRequest
{
    public sealed class LookupMusicRequestCaptureHandler(LookupMusicRequestCapture capture)
    {
        [WolverineHandler]
        public Task Handle(LookupMusicRequestDto requestDto, CancellationToken cancellationToken)
        {
            capture.Record(requestDto);
            return Task.CompletedTask;
        }
    }
}

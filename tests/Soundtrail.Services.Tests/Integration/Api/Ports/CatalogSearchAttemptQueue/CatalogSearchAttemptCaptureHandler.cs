using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Wolverine.Attributes;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearchAttemptQueue
{
    public sealed class CatalogSearchAttemptCaptureHandler(CatalogSearchAttemptCapture capture)
    {
        [WolverineHandler]
        public Task Handle(CatalogSearchAttemptDto requestDto, CancellationToken cancellationToken)
        {
            capture.Record(requestDto);
            return Task.CompletedTask;
        }
    }
}

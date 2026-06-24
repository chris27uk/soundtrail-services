using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Wolverine.Attributes;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

public sealed class PipelineMessageCaptureHandler(PipelineMessageCapture capture)
{
    [WolverineHandler]
    public Task Handle(CatalogSearchAttemptDto message, CancellationToken cancellationToken)
    {
        capture.Record(message);
        return Task.CompletedTask;
    }

    [WolverineHandler]
    public Task Handle(LookupCanonicalMusicMetadataCommandDto message, CancellationToken cancellationToken)
    {
        capture.Record(message);
        return Task.CompletedTask;
    }

    [WolverineHandler]
    public Task Handle(MusicCatalogMetadataFetchedDto message, CancellationToken cancellationToken)
    {
        capture.Record(message);
        return Task.CompletedTask;
    }

    [WolverineHandler]
    public Task Handle(StreamingLocationsRequiredMessageDto message, CancellationToken cancellationToken)
    {
        capture.Record(message);
        return Task.CompletedTask;
    }

    [WolverineHandler]
    public Task Handle(LookupStreamingLocationsCommandDto message, CancellationToken cancellationToken)
    {
        capture.Record(message);
        return Task.CompletedTask;
    }
}

using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnWorkRequested;
using Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class DiscoveryProjectionDispatcher(
    StoredEventDomainEventResolver resolver,
    WorkRequestedProjectorHandler workRequestedProjectorHandler,
    WorkScheduledProjectorHandler workScheduledProjectorHandler,
    WorkFeedbackChangedProjectorHandler workFeedbackChangedProjectorHandler)
{
    private readonly EventHandlers handlers = BuildHandlers(
        workRequestedProjectorHandler,
        workScheduledProjectorHandler,
        workFeedbackChangedProjectorHandler);

    public Task DispatchAsync(RavenStoredEventRecord storedEvent, CancellationToken cancellationToken) =>
        handlers.HandleAsync(resolver.Resolve(storedEvent), cancellationToken);

    private static EventHandlers BuildHandlers(
        WorkRequestedProjectorHandler workRequestedProjectorHandler,
        WorkScheduledProjectorHandler workScheduledProjectorHandler,
        WorkFeedbackChangedProjectorHandler workFeedbackChangedProjectorHandler)
    {
        var handlers = new EventHandlers();

        handlers.RegisterAsync<WorkRequested>(workRequestedProjectorHandler.Handle);
        handlers.RegisterAsync<WorkRequested>(workFeedbackChangedProjectorHandler.Handle);
        handlers.RegisterAsync<WorkPriorityRaised>(workRequestedProjectorHandler.Handle);
        handlers.RegisterAsync<WorkScheduled>(workScheduledProjectorHandler.Handle);
        handlers.RegisterAsync<WorkScheduled>(workFeedbackChangedProjectorHandler.Handle);
        handlers.RegisterAsync<WorkDeferred>(workFeedbackChangedProjectorHandler.Handle);
        handlers.RegisterAsync<WorkCompleted>(workFeedbackChangedProjectorHandler.Handle);
        handlers.RegisterAsync<WorkRejected>(workFeedbackChangedProjectorHandler.Handle);
        handlers.RegisterAsync<WorkIgnored>(workFeedbackChangedProjectorHandler.Handle);
        handlers.RegisterAsync<WorkAttemptFailed>(workFeedbackChangedProjectorHandler.Handle);

        return handlers;
    }
}

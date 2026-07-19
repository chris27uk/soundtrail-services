using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

internal sealed class WorkFeedbackChangedCdcService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/work-feedback-changed";

    protected override Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "catalog-stream"
            && (x.EventType == "work-requested"
                || x.EventType == "work-scheduled"
                || x.EventType == "work-deferred"
                || x.EventType == "work-completed"
                || x.EventType == "work-rejected"
                || x.EventType == "work-ignored"
                || x.EventType == "work-attempt-failed");

    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<WorkFeedbackChangedProjectorHandler>();

        switch (storedEvent.EventType)
        {
            case "work-requested":
                await handler.Handle(
                    TypeTranslationRegistry.Default.ToDomainObject<WorkRequested>(
                        storedEvent.Body ?? throw new InvalidOperationException("WorkRequested events must include a body.")),
                    cancellationToken);
                break;
            case "work-scheduled":
                await handler.Handle(
                    TypeTranslationRegistry.Default.ToDomainObject<WorkScheduled>(
                        storedEvent.Body ?? throw new InvalidOperationException("WorkScheduled events must include a body.")),
                    cancellationToken);
                break;
            case "work-deferred":
                await handler.Handle(
                    TypeTranslationRegistry.Default.ToDomainObject<WorkDeferred>(
                        storedEvent.Body ?? throw new InvalidOperationException("WorkDeferred events must include a body.")),
                    cancellationToken);
                break;
            case "work-completed":
                await handler.Handle(
                    TypeTranslationRegistry.Default.ToDomainObject<WorkCompleted>(
                        storedEvent.Body ?? throw new InvalidOperationException("WorkCompleted events must include a body.")),
                    cancellationToken);
                break;
            case "work-rejected":
                await handler.Handle(
                    TypeTranslationRegistry.Default.ToDomainObject<WorkRejected>(
                        storedEvent.Body ?? throw new InvalidOperationException("WorkRejected events must include a body.")),
                    cancellationToken);
                break;
            case "work-ignored":
                await handler.Handle(
                    TypeTranslationRegistry.Default.ToDomainObject<WorkIgnored>(
                        storedEvent.Body ?? throw new InvalidOperationException("WorkIgnored events must include a body.")),
                    cancellationToken);
                break;
            case "work-attempt-failed":
                await handler.Handle(
                    TypeTranslationRegistry.Default.ToDomainObject<WorkAttemptFailed>(
                        storedEvent.Body ?? throw new InvalidOperationException("WorkAttemptFailed events must include a body.")),
                    cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unsupported work feedback event type '{storedEvent.EventType}'.");
        }
    }
}

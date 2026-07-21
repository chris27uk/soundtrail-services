using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkRequested.Adapters;

internal sealed class WorkRequestedCdcService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/work-requested";

    protected override Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "catalog-stream"
             && (x.EventType == "work-requested" || x.EventType == "work-priority-raised");

    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<WorkRequestedProjectorHandler>();
        var body = storedEvent.Body ?? throw new InvalidOperationException("Discovery work demand events must include a body.");
        if (storedEvent.EventType == "work-requested")
        {
            var workRequested = TypeTranslationRegistry.Default.ToDomainObject<WorkRequested>(body);
            await handler.Handle(workRequested, cancellationToken);
            return;
        }

        var priorityRaised = TypeTranslationRegistry.Default.ToDomainObject<WorkPriorityRaised>(body);
        await handler.Handle(priorityRaised, cancellationToken);
    }
}

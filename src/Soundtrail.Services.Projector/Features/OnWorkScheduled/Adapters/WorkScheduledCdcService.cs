using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled.Adapters;

internal sealed class WorkScheduledCdcService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/work-scheduled";

    protected override Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "catalog-stream"
            && x.ProjectionHint != "bulk-import"
            && x.EventType == "work-scheduled";

    protected override bool IsSubscriptionDefinitionStale(Raven.Client.Documents.Subscriptions.SubscriptionState state) =>
        !state.Query.Contains("bulk-import", StringComparison.Ordinal);
    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<WorkScheduledProjectorHandler>();
        var workScheduled = TypeTranslationRegistry.Default.ToDomainObject<WorkScheduled>(
            storedEvent.Body ?? throw new InvalidOperationException("WorkScheduled events must include a body."));

        await handler.Handle(workScheduled, cancellationToken);
    }
}

using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class DiscoveryProjectionSubscriptionService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/discovery";

    protected override Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "catalog-stream"
             && (x.EventType == "work-requested"
                 || x.EventType == "work-priority-raised"
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
        var dispatcher = serviceProvider.GetRequiredService<DiscoveryProjectionDispatcher>();
        await dispatcher.DispatchAsync(storedEvent, cancellationToken);
    }
}

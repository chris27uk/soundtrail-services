using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class DiscoveryProjectionSubscriptionService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore,
    ILogger<RavenEventSubscriptionBackgroundService> logger) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore, logger)
{
    protected override string SubscriptionName => DiscoveryProjectionDispatcher.SubscriptionName;

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

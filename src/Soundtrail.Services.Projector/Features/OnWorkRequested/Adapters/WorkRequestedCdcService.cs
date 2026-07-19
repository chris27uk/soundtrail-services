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
        x => x.AggregateType == "catalog-stream" && x.EventType == "work-requested";

    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<WorkRequestedProjectorHandler>();
        var workRequested = TypeTranslationRegistry.Default.ToDomainObject<WorkRequested>(
            storedEvent.Body ?? throw new InvalidOperationException("WorkRequested events must include a body."));

        await handler.Handle(workRequested, cancellationToken);
    }
}

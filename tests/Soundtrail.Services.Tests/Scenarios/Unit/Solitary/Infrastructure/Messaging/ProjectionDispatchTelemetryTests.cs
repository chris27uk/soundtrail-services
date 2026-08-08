using System.Diagnostics;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Projection;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

public sealed class ProjectionDispatchTelemetryTests
{
    [Fact]
    public async Task Given_A_Stored_Event_When_Dispatching_Then_Handle_Message_Activity_Is_Emitted()
    {
        using var probe = ActivityProbe.Start();
        var domainEvent = CreateWorkRequested("corr-proj");
        var storedEvent = new RavenStoredEventRecord
        {
            Id = "events/1",
            EventId = "event-1",
            EventType = "work-requested",
            BodyType = "WorkRequestedDto",
            AggregateType = "catalog-stream",
            CorrelationId = "corr-proj",
            Body = new CatalogDiscoveryWorkRequestedEventDataRecordDto(
                ResourceKind: "search",
                ResourceValue: "nirvana",
                ResourceItemKind: null,
                Priority: "High",
                TrustLevel: 1,
                RiskScore: 2,
                RequestedAtUtc: new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero),
                CorrelationId: "corr-proj")
        };
        var handlers = new HandlerCollection();
        var handler = new ProjectionHandlerSpy();
        handlers.Register<WorkRequested>(handler.HandleAsync);
        var dispatcher = new DiscoveryProjectionDispatcher(
            new StoredEventDomainEventResolver(new TypeRegistryStub(domainEvent)),
            handlers);

        await dispatcher.DispatchAsync(storedEvent, CancellationToken.None);

        handler.Calls.Should().Be(1);
        probe.LastStoppedActivity.Should().NotBeNull();
        probe.LastStoppedActivity!.OperationName.Should().Be(MessageTelemetry.HandleMessageActivityName);
        probe.LastStoppedActivity.GetTagItem("soundtrail.dto_type_name").Should().Be("WorkRequestedDto");
        probe.LastStoppedActivity.GetTagItem("soundtrail.domain_event_name").Should().Be(typeof(WorkRequested).FullName);
        probe.LastStoppedActivity.GetTagItem("soundtrail.correlation_id").Should().Be("corr-proj");
        probe.LastStoppedActivity.GetTagItem("soundtrail.queue_name").Should().Be(DiscoveryProjectionDispatcher.SubscriptionName);
        probe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain(MessageTelemetry.HandleMessageActivityName);
        probe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("message.processed");
    }

    [Fact]
    public async Task Given_A_Projection_Telemetry_Decorator_When_Handling_Then_It_Emits_Started_And_Completed()
    {
        using var probe = ActivityProbe.Start();
        var inner = new ProjectionHandlerSpy();
        var decorator = new TelemetryProjectionEventHandlerDecorator<WorkRequested>(inner);

        await decorator.HandleAsync(CreateWorkRequested("corr-2"));

        inner.Calls.Should().Be(1);
        probe.LastStoppedActivity.Should().NotBeNull();
        probe.LastStoppedActivity!.Events.Select(x => x.Name).Should().Contain("work-requested.started");
        probe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("work-requested.completed");
    }

    private static WorkRequested CreateWorkRequested(string correlationId) =>
        new(
            new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria("nirvana")),
            LookupPriorityBand.High,
            TrustLevel: 1,
            RiskScore: 2,
            RequestedAt: new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From(correlationId));

    private sealed class ProjectionHandlerSpy : IProjectionEventHandler<WorkRequested>
    {
        public int Calls { get; private set; }

        public Task HandleAsync(WorkRequested @event, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class TypeRegistryStub(IDomainEvent domainEvent) : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => throw new NotSupportedException();

        public object ToDto(object domainObject) => throw new NotSupportedException();

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class =>
            (TDomain)domainEvent;

        public object ToDomainObject(object? dto) => domainEvent;

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }

    private sealed class ActivityProbe : IDisposable
    {
        private readonly ActivityListener listener;

        private ActivityProbe(ActivityListener listener)
        {
            this.listener = listener;
        }

        public Activity? LastStoppedActivity { get; private set; }

        public static ActivityProbe Start()
        {
            ActivityProbe? probe = null;
            var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "Soundtrail.Messaging",
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => probe!.LastStoppedActivity = activity
            };

            ActivitySource.AddActivityListener(listener);
            probe = new ActivityProbe(listener);
            return probe;
        }

        public void Dispose() => this.listener.Dispose();
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Messaging.Asb;
using Soundtrail.Adapters.Messaging.Contracts;
using Soundtrail.Adapters.Projection;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using System.Diagnostics;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

public sealed class IncomingMessageSessionTests
{
    [Fact]
    public async Task Given_A_Valid_Message_When_Processing_Then_It_Invokes_The_Handler_With_Metadata_And_Completes()
    {
        var environment = TestEnvironment.Create();
        var session = environment.CreateSession();
        var envelope = environment.CreateEnvelope(retryCount: 2);

        await session.ProcessAsync(envelope, environment.Lifecycle, CancellationToken.None);

        environment.Handler.Invocations.Should().ContainSingle();
        var invocation = environment.Handler.Invocations.Single();
        invocation.Message.Should().BeSameAs(environment.DomainMessage);
        invocation.Metadata.MessageId.Should().Be("message-123");
        invocation.Metadata.CorrelationId.Should().Be("corr-123");
        invocation.Metadata.QueueName.Should().Be(ServiceBusQueues.KnownMusicDataRequests);
        invocation.Metadata.RetryCount.Should().Be(2);
        environment.Lifecycle.Completed.Should().BeTrue();
        environment.Lifecycle.RetryDelay.Should().BeNull();
        environment.Lifecycle.DeadLetterReason.Should().BeNull();
    }

    [Fact]
    public async Task Given_A_Handler_That_Replies_When_Processing_Then_Reply_Uses_Command_Bus_And_Enriches_Activity()
    {
        using var activityProbe = ActivityProbe.Start();
        var environment = TestEnvironment.Create(replyWith: new ReplyMessage());
        var session = environment.CreateSession();

        await session.ProcessAsync(environment.CreateEnvelope(), environment.Lifecycle, CancellationToken.None);

        environment.CommandBus.Messages.Should().ContainSingle();
        environment.CommandBus.Messages.Single().Should().BeOfType<ReplyMessage>();
        environment.CommandBus.ReplyMessageTypeTag.Should().Be(typeof(ReplyMessage).FullName);
        environment.CommandBus.ReplyCorrelationIdTag.Should().Be("reply-corr");
        activityProbe.LastStoppedActivity!.Events.Select(x => x.Name).Should().Contain("message.replying");
    }

    [Fact]
    public async Task Given_A_Handler_Exception_When_Retry_Is_Available_Then_It_Retries()
    {
        using var activityProbe = ActivityProbe.Start();
        var environment = TestEnvironment.Create(handlerException: new InvalidOperationException("boom"));
        var session = environment.CreateSession();

        await session.ProcessAsync(environment.CreateEnvelope(retryCount: 1), environment.Lifecycle, CancellationToken.None);

        environment.Lifecycle.Completed.Should().BeFalse();
        environment.Lifecycle.RetryDelay.Should().NotBeNull();
        environment.Lifecycle.DeadLetterReason.Should().BeNull();
        activityProbe.LastStoppedActivity!.Status.Should().Be(ActivityStatusCode.Error);
        activityProbe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("exception");
        activityProbe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("message.retried");
    }

    [Fact]
    public async Task Given_A_Handler_Exception_When_Retries_Are_Exhausted_Then_It_Dead_Letters()
    {
        using var activityProbe = ActivityProbe.Start();
        var environment = TestEnvironment.Create(handlerException: new InvalidOperationException("boom"));
        var session = environment.CreateSession();

        await session.ProcessAsync(environment.CreateEnvelope(retryCount: 5), environment.Lifecycle, CancellationToken.None);

        environment.Lifecycle.Completed.Should().BeFalse();
        environment.Lifecycle.RetryDelay.Should().BeNull();
        environment.Lifecycle.DeadLetterReason.Should().Be("MessageProcessingFailed");
        environment.Lifecycle.DeadLetterDescription.Should().Contain("boom");
        activityProbe.LastStoppedActivity!.Status.Should().Be(ActivityStatusCode.Error);
        activityProbe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("exception");
        activityProbe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("message.dead_lettered");
    }

    [Fact]
    public void Given_A_Transport_Envelope_When_Starting_Handle_Activity_Then_Core_Handle_Tags_Are_Present()
    {
        using var activityProbe = ActivityProbe.Start();
        var envelope = TestEnvironment.Create().CreateEnvelope(retryCount: 3);

        using var activity = MessageTelemetry.StartHandleActivity(
            envelope,
            typeof(DtoMessage),
            typeof(DomainMessage));
        MessageTelemetry.RecordHandleMessageEvent(activity);

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be(MessageTelemetry.HandleMessageActivityName);
        activity.GetTagItem("soundtrail.dto_type_name").Should().Be(typeof(DtoMessage).FullName);
        activity.GetTagItem("soundtrail.domain_event_name").Should().Be(typeof(DomainMessage).FullName);
        activity.GetTagItem("soundtrail.correlation_id").Should().Be("corr-123");
        activity.GetTagItem("messaging.conversation_id").Should().Be("corr-123");
        activity.GetTagItem("soundtrail.queue_name").Should().Be(ServiceBusQueues.KnownMusicDataRequests);
        activity.GetTagItem("soundtrail.is_retry").Should().Be(true);
        activity.GetTagItem("soundtrail.retry_count").Should().Be(3);
        activity.GetTagItem("messaging.message.id").Should().Be("message-123");
        activity.Events.Select(x => x.Name).Should().Contain(MessageTelemetry.HandleMessageActivityName);
    }

    [Fact]
    public void Given_A_Dto_Type_When_Resolving_Domain_Event_Name_Then_It_Is_Rejected()
    {
        MessageTelemetry.IsTransportDtoType(typeof(SampleCommandDto)).Should().BeTrue();
        MessageTelemetry.DomainEventNameFor(typeof(SampleCommandDto)).Should().BeNull();
        MessageTelemetry.DomainEventNameFor(typeof(DomainMessage)).Should().Be(typeof(DomainMessage).FullName);
    }

    [Fact]
    public void Given_A_Message_When_Starting_Publish_Activity_Then_Core_Publish_Tags_Are_Present()
    {
        using var activityProbe = ActivityProbe.Start();
        var message = new DomainMessage();
        var dto = new DtoMessage("dto-123");

        using var activity = MessageTelemetry.StartPublishActivity(
            message,
            dto,
            ServiceBusQueues.KnownMusicDataRequests);

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be(MessageTelemetry.PublishMessageActivityName);
        activity.GetTagItem("soundtrail.dto_type_name").Should().Be(typeof(DtoMessage).FullName);
        activity.GetTagItem("soundtrail.domain_event_name").Should().Be(typeof(DomainMessage).FullName);
        activity.GetTagItem("soundtrail.correlation_id").Should().Be(message.CorrelationId.Value);
        activity.GetTagItem("soundtrail.queue_name").Should().Be(ServiceBusQueues.KnownMusicDataRequests);
        activity.Events.Select(x => x.Name).Should().Contain(MessageTelemetry.PublishMessageActivityName);
    }

    [Fact]
    public void Given_A_Targeted_And_Prioritised_Message_When_Starting_Handler_Activity_Then_Target_And_Risk_Tags_Are_Present()
    {
        using var activityProbe = ActivityProbe.Start();
        var message = new TargetedPrioritisedMessage();

        using var activity = MessageTelemetry.StartHandlerActivity(message, "test-stage");

        activity.Should().NotBeNull();
        activity!.GetTagItem("soundtrail.workflow_stage").Should().Be("test-stage");
        activity.GetTagItem("message.id").Should().Be("telemetry-123");
        activity.GetTagItem("messaging.conversation_id").Should().Be("telemetry-corr");
        activity.GetTagItem("soundtrail.domain_event_name").Should().Be(typeof(TargetedPrioritisedMessage).FullName);
        activity.GetTagItem("soundtrail.target").Should().Be(message.Target.NormalisedIdentifier);
        activity.GetTagItem("soundtrail.target_kind").Should().Be(message.Target.GetType().Name);
        activity.GetTagItem("soundtrail.trust_level").Should().Be(88);
        activity.GetTagItem("soundtrail.risk_score").Should().Be(7);
    }

    [Fact]
    public async Task Given_A_Valid_Message_When_Processing_Then_Handle_Activity_Includes_Handle_Message_Event()
    {
        using var activityProbe = ActivityProbe.Start();
        var environment = TestEnvironment.Create();
        var session = environment.CreateSession();

        await session.ProcessAsync(environment.CreateEnvelope(retryCount: 2), environment.Lifecycle, CancellationToken.None);

        activityProbe.LastStoppedActivity.Should().NotBeNull();
        activityProbe.LastStoppedActivity!.OperationName.Should().Be(MessageTelemetry.HandleMessageActivityName);
        activityProbe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain(MessageTelemetry.HandleMessageActivityName);
        activityProbe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("message.processed");
        activityProbe.LastStoppedActivity.GetTagItem("soundtrail.is_retry").Should().Be(true);
        activityProbe.LastStoppedActivity.GetTagItem("soundtrail.retry_count").Should().Be(2);
    }

    [Fact]
    public async Task Given_A_Telemetry_Decorator_When_Handling_Then_It_Emits_Started_And_Completed_Events()
    {
        using var activityProbe = ActivityProbe.Start();
        var inner = new HandlerSpy(null, null);
        var decorator = new TelemetryHandlerDecorator<DomainMessage>(inner);
        var message = new DomainMessage();

        await decorator.Handle(IncomingMessage<DomainMessage>.Create(message));

        inner.Invocations.Should().ContainSingle();
        activityProbe.LastStoppedActivity.Should().NotBeNull();
        activityProbe.LastStoppedActivity!.Events.Select(x => x.Name).Should().Contain("domain.started");
        activityProbe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("domain.completed");
    }

    private sealed class TestEnvironment
    {
        private TestEnvironment(
            ServiceProvider serviceProvider,
            HandlerSpy handler,
            CommandBusSpy commandBus,
            DtoMessage dtoMessage,
            DomainMessage domainMessage,
            TestLifecycle lifecycle)
        {
            ServiceProvider = serviceProvider;
            Handler = handler;
            CommandBus = commandBus;
            DtoMessage = dtoMessage;
            DomainMessage = domainMessage;
            Lifecycle = lifecycle;
        }

        public ServiceProvider ServiceProvider { get; }

        public HandlerSpy Handler { get; }

        public CommandBusSpy CommandBus { get; }

        public DtoMessage DtoMessage { get; }

        public DomainMessage DomainMessage { get; }

        public TestLifecycle Lifecycle { get; }

        public static TestEnvironment Create(
            IMessage? replyWith = null,
            Exception? handlerException = null)
        {
            var dtoMessage = new DtoMessage("dto-123");
            var domainMessage = new DomainMessage();
            var handler = new HandlerSpy(replyWith, handlerException);
            var commandBus = new CommandBusSpy();
            var lifecycle = new TestLifecycle();

            var services = new ServiceCollection();
            services.AddScoped<ITypeRegistry>(_ => new TypeRegistryStub(dtoMessage, domainMessage));
            services.AddScoped<IHandler<DomainMessage>>(_ => handler);
            services.AddScoped<ICommandBus>(_ => commandBus);
            services.AddScoped(sp =>
            {
                var collection = new HandlerCollection();
                collection.RegisterHandler(sp.GetRequiredService<IHandler<DomainMessage>>());
                return collection;
            });

            return new TestEnvironment(
                services.BuildServiceProvider(),
                handler,
                commandBus,
                dtoMessage,
                domainMessage,
                lifecycle);
        }

        public IncomingMessageSession<DtoMessage, DomainMessage> CreateSession()
        {
            return new IncomingMessageSession<DtoMessage, DomainMessage>(
                new DeserializerStub(DtoMessage),
                ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                new ExponentialRetryPolicy(),
                NullLogger<IncomingMessageSession<DtoMessage, DomainMessage>>.Instance);
        }

        public TransportEnvelope CreateEnvelope(int retryCount = 0)
        {
            return new TransportEnvelope(
                new BinaryData("{\"id\":\"dto-123\"}"),
                new MessageMetadata(
                    "message-123",
                    "corr-123",
                    "reply-queue",
                    ServiceBusQueues.KnownMusicDataRequests,
                    retryCount,
                    new Dictionary<string, object?> { ["x-test"] = "true" }),
                "azure_service_bus",
                typeof(DtoMessage),
                1);
        }
    }

    private sealed class DeserializerStub(DtoMessage dtoMessage) : IMessageBodyDeserializer
    {
        public TMessage Deserialize<TMessage>(BinaryData body)
        {
            return (TMessage)(object)dtoMessage;
        }
    }

    private sealed class TypeRegistryStub(DtoMessage dtoMessage, DomainMessage domainMessage) : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => throw new NotSupportedException();

        public object ToDto(object domainObject) => throw new NotSupportedException();

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class
        {
            dto.Should().BeSameAs(dtoMessage);
            return (TDomain)(object)domainMessage;
        }

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }

    private sealed class HandlerSpy(IMessage? replyWith, Exception? exceptionToThrow) : IHandler<DomainMessage>
    {
        public List<IncomingMessage<DomainMessage>> Invocations { get; } = [];

        public async Task Handle(IncomingMessage<DomainMessage> context, CancellationToken cancellationToken = default)
        {
            Invocations.Add(context);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            if (replyWith is not null)
            {
                await context.ReplyAsync(replyWith, cancellationToken);
            }
        }
    }

    private sealed class CommandBusSpy : ICommandBus
    {
        public List<IMessage> Messages { get; } = [];

        public string? ReplyMessageTypeTag { get; private set; }

        public string? ReplyCorrelationIdTag { get; private set; }

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            ReplyMessageTypeTag = Activity.Current?.Tags.FirstOrDefault(x => x.Key == "soundtrail.reply.message_type").Value;
            ReplyCorrelationIdTag = Activity.Current?.Tags.FirstOrDefault(x => x.Key == "soundtrail.reply.correlation_id").Value;
            return Task.CompletedTask;
        }
    }

    private sealed class TestLifecycle : IMessageLifecycle
    {
        public bool Completed { get; private set; }

        public TimeSpan? RetryDelay { get; private set; }

        public string? DeadLetterReason { get; private set; }

        public string? DeadLetterDescription { get; private set; }

        public Task CompleteAsync(CancellationToken cancellationToken)
        {
            Completed = true;
            return Task.CompletedTask;
        }

        public Task RetryAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            RetryDelay = delay;
            return Task.CompletedTask;
        }

        public Task DeadLetterAsync(string reason, string description, CancellationToken cancellationToken)
        {
            DeadLetterReason = reason;
            DeadLetterDescription = description;
            return Task.CompletedTask;
        }
    }

    private sealed record DtoMessage(string Id);

    private sealed record SampleCommandDto(string Id);

    private sealed record DomainMessage : IMessage
    {
        public MessageId Id { get; init; } = MessageId.For("domain-123");

        public CorrelationId CorrelationId { get; init; } = CorrelationId.From("domain-corr");

        public DateTimeOffset RequestedAt { get; init; } = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed record ReplyMessage : IMessage
    {
        public MessageId Id { get; init; } = MessageId.For("reply-123");

        public CorrelationId CorrelationId { get; init; } = CorrelationId.From("reply-corr");

        public DateTimeOffset RequestedAt { get; init; } = new(2026, 7, 28, 12, 1, 0, TimeSpan.Zero);
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

        public void Dispose()
        {
            this.listener.Dispose();
        }
    }

    private sealed record TargetedPrioritisedMessage : ITargetedMessage, IPrioritisedMessage
    {
        public MessageId Id { get; init; } = MessageId.For("telemetry-123");

        public CorrelationId CorrelationId { get; init; } = CorrelationId.From("telemetry-corr");

        public DateTimeOffset RequestedAt { get; init; } = new(2026, 7, 28, 12, 2, 0, TimeSpan.Zero);

        public EnrichmentTarget Target { get; init; } =
            new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria("nirvana"));

        public int? RiskScore { get; init; } = 7;

        public int? TrustLevel { get; init; } = 88;
    }
}

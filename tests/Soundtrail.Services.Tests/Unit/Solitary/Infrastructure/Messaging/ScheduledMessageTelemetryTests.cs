using System.Diagnostics;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

public sealed class ScheduledMessageTelemetryTests
{
    [Fact]
    public async Task Given_A_Scheduled_Command_When_Handling_Then_Handle_Message_Activity_Is_Emitted()
    {
        using var probe = ActivityProbe.Start();
        var handler = new HandlerSpy();

        await ScheduledMessageTelemetry.HandleAsync(
            handler,
            new ImportKworbChartCommand(DateTimeOffset.UtcNow),
            ImportKworbChartTickerFunctions.FunctionName);

        handler.Calls.Should().Be(1);
        probe.LastStoppedActivity.Should().NotBeNull();
        probe.LastStoppedActivity!.OperationName.Should().Be(MessageTelemetry.HandleMessageActivityName);
        probe.LastStoppedActivity.GetTagItem("soundtrail.dto_type_name").Should().Be(typeof(ImportKworbChartCommand).FullName);
        probe.LastStoppedActivity.GetTagItem("soundtrail.domain_event_name").Should().Be(typeof(ImportKworbChartCommand).FullName);
        probe.LastStoppedActivity.GetTagItem("soundtrail.queue_name").Should().Be(ImportKworbChartTickerFunctions.FunctionName);
        probe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain(MessageTelemetry.HandleMessageActivityName);
        probe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("message.processed");
    }

    [Fact]
    public async Task Given_A_Telemetry_Decorator_For_Non_Message_Command_When_Handling_Then_It_Emits_Stage_Events()
    {
        using var probe = ActivityProbe.Start();
        var inner = new HandlerSpy();
        var decorator = new TelemetryHandlerDecorator<ImportKworbChartCommand>(inner);

        await decorator.Handle(
            IncomingMessage<ImportKworbChartCommand>.Create(new ImportKworbChartCommand(DateTimeOffset.UtcNow)));

        inner.Calls.Should().Be(1);
        probe.LastStoppedActivity.Should().NotBeNull();
        probe.LastStoppedActivity!.Events.Select(x => x.Name).Should().Contain("import-kworb-chart.started");
        probe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("import-kworb-chart.completed");
    }

    private sealed class HandlerSpy : IHandler<ImportKworbChartCommand>
    {
        public int Calls { get; private set; }

        public Task Handle(IncomingMessage<ImportKworbChartCommand> context, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
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

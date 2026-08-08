using System.Diagnostics;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

public sealed class ScheduledMessageTelemetryTests
{
    [Fact]
    public async Task Given_A_Scheduled_Command_When_Executing_Then_Handle_Message_Activity_Is_Emitted()
    {
        using var probe = ActivityProbe.Start();
        var calls = 0;

        await ScheduledMessageTelemetry.ExecuteAsync(
            new ImportKworbChartCommand(DateTimeOffset.UtcNow),
            ImportKworbChartTickerFunctions.FunctionName,
            (_, _) =>
            {
                calls++;
                return Task.CompletedTask;
            });

        calls.Should().Be(1);
        probe.LastStoppedActivity.Should().NotBeNull();
        probe.LastStoppedActivity!.OperationName.Should().Be(MessageTelemetry.HandleMessageActivityName);
        probe.LastStoppedActivity.GetTagItem("soundtrail.dto_type_name").Should().BeNull();
        probe.LastStoppedActivity.GetTagItem("soundtrail.domain_event_name").Should().Be(typeof(ImportKworbChartCommand).FullName);
        probe.LastStoppedActivity.GetTagItem("soundtrail.queue_name").Should().Be(ImportKworbChartTickerFunctions.FunctionName);
        probe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain(MessageTelemetry.HandleMessageActivityName);
        probe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("message.processed");
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

using System.Diagnostics;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

[Collection(nameof(ActivityTelemetryCollection))]
public sealed class TelemetryScheduledMessageHandlerDecoratorTests
{
    [Fact]
    public async Task Given_A_Scheduled_Handler_When_Handling_Then_It_Emits_ScheduleTriggered()
    {
        using var probe = ActivityProbe.Start();
        var decorator = new TelemetryScheduledMessageHandlerDecorator<ImportKworbChartCommand>(new NoOpHandler());

        await decorator.HandleAsync(new ImportKworbChartCommand(new DateTimeOffset(2026, 7, 19, 10, 23, 0, TimeSpan.Zero)));

        probe.LastStoppedActivity.Should().NotBeNull();
        probe.LastStoppedActivity!.Events.Select(x => x.Name)
            .Should()
            .Contain(MessageTelemetry.ScheduleTriggeredEventName);
        probe.LastStoppedActivity.GetTagItem("soundtrail.message_type")
            .Should()
            .Be(typeof(ImportKworbChartCommand).FullName);
        probe.LastStoppedActivity.GetTagItem("soundtrail.triggered_at_utc")
            .Should()
            .Be(new DateTimeOffset(2026, 7, 19, 10, 23, 0, TimeSpan.Zero).UtcDateTime);
    }

    [Fact]
    public async Task Given_A_Failing_Scheduled_Handler_When_Handling_Then_It_Marks_The_Activity_As_Error()
    {
        using var probe = ActivityProbe.Start();
        var decorator = new TelemetryScheduledMessageHandlerDecorator<ImportKworbChartCommand>(new FailingHandler());

        var act = () => decorator.HandleAsync(new ImportKworbChartCommand(DateTimeOffset.UtcNow));

        await act.Should().ThrowAsync<InvalidOperationException>();
        probe.LastStoppedActivity.Should().NotBeNull();
        probe.LastStoppedActivity!.Status.Should().Be(ActivityStatusCode.Error);
        probe.LastStoppedActivity.Events.Select(x => x.Name).Should().Contain("exception");
    }

    private sealed class NoOpHandler : IScheduledMessageHandler<ImportKworbChartCommand>
    {
        public Task HandleAsync(ImportKworbChartCommand message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FailingHandler : IScheduledMessageHandler<ImportKworbChartCommand>
    {
        public Task HandleAsync(ImportKworbChartCommand message, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("scheduled failure");
    }

}

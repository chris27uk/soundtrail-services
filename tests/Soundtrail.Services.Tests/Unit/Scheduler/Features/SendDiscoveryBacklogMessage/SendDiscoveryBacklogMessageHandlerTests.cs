using FluentAssertions;
using Soundtrail.Services.Enrichment.Scheduler;
using Soundtrail.Services.Enrichment.Scheduler.Features.SendDiscoveryBacklogMessage;
using Soundtrail.Services.Enrichment.Scheduler.Features.SendDiscoveryBacklogMessage.Support;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Scheduler.Features.SendDiscoveryBacklogMessage;

public sealed class SendDiscoveryBacklogMessageHandlerTests
{
    [Fact]
    public async Task Given_A_Scheduling_Request_When_Handled_Then_The_Discovery_Backlog_Message_Is_Sent()
    {
        var bus = new CommandBusFake();
        var now = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);
        var handler = new SendDiscoveryBacklogMessageHandler(bus, new TestTimeProvider(now));

        await handler.Handle(CancellationToken.None);

        var message = bus.SentCommands.Single().Should().BeOfType<RunDiscoveryBacklogSchedulingCommand>().Subject;
        message.CommandId.Value.Should().Be($"RunDiscoveryBacklogScheduling:{now.ToUnixTimeMilliseconds()}");
        message.CorrelationId.Value.Should().NotBeNullOrWhiteSpace();
        message.Priority.Should().Be(Soundtrail.Contracts.Common.LookupPriorityBand.Low);
        message.CreatedAt.Should().Be(now);
        message.Now.Should().Be(now);
        message.Take.Should().Be(25);
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : ITimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}

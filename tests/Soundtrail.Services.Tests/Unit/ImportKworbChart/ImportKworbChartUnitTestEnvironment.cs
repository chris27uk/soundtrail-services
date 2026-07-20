using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

internal sealed class ImportKworbChartUnitTestEnvironment
{
    private ImportKworbChartUnitTestEnvironment()
    {
        CommandBus = new CommandBusFake();
    }

    public CommandBusFake CommandBus { get; }

    public static ImportKworbChartUnitTestEnvironment Create() => new();

    public ImportKworbChartHandler CreateSubjectUnderTest() => new(CommandBus);

    public ImportKworbChartCommand CreateRequest(DateTimeOffset? triggeredAt = null) =>
        new(triggeredAt ?? new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

    public sealed class CommandBusFake : ICommandBus
    {
        public List<RequestKnownMusicDataMessage> Commands { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Commands.Add((RequestKnownMusicDataMessage)message);
            return Task.CompletedTask;
        }
    }
}

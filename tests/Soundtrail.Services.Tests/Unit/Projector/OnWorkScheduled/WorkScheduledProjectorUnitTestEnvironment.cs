using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled;

namespace Soundtrail.Services.Tests.Unit.Projector.OnWorkScheduled;

internal sealed class WorkScheduledProjectorUnitTestEnvironment
{
    private WorkScheduledProjectorUnitTestEnvironment(
        CommandBusFake commandBus)
    {
        CommandBus = commandBus;
    }

    public CommandBusFake CommandBus { get; }

    public static WorkScheduledProjectorUnitTestEnvironment Create() =>
        new(new CommandBusFake());

    public WorkScheduledProjectorHandler CreateSubject() => new(CommandBus);

    public static WorkScheduled CreateEvent(
        EnrichmentTarget? target = null,
        LookupPriorityBand priority = LookupPriorityBand.High,
        DateTimeOffset? scheduledAt = null) =>
        new(
            target ?? Work.EnrichTrackStreamingLocation(TestTrackIds.Create("track-2901")),
            priority,
            new DateTimeOffset(2026, 7, 18, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 18, 9, 2, 0, TimeSpan.Zero),
            "Planner selected the work.",
            scheduledAt ?? new DateTimeOffset(2026, 7, 18, 8, 59, 0, TimeSpan.Zero));

    public sealed class CommandBusFake : ICommandBus
    {
        public List<IMessage> Commands { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Commands.Add(message);
            return Task.CompletedTask;
        }
    }
}

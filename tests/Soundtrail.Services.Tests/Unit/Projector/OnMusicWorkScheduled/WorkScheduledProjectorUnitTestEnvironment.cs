using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnMusicWorkScheduled;

namespace Soundtrail.Services.Tests.Unit.Projector.OnMusicWorkScheduled;

internal sealed class WorkScheduledProjectorUnitTestEnvironment
{
    private WorkScheduledProjectorUnitTestEnvironment(
        CommandBusFake commandBus,
        StoreDiscoveryFeedbackPortFake storeDiscoveryFeedbackPort)
    {
        CommandBus = commandBus;
        StoreDiscoveryFeedbackPort = storeDiscoveryFeedbackPort;
    }

    public CommandBusFake CommandBus { get; }

    public StoreDiscoveryFeedbackPortFake StoreDiscoveryFeedbackPort { get; }

    public static WorkScheduledProjectorUnitTestEnvironment Create() =>
        new(new CommandBusFake(), new StoreDiscoveryFeedbackPortFake());

    public WorkScheduledProjectorHandler CreateSubject() => new(CommandBus, StoreDiscoveryFeedbackPort);

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
        public List<ICommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }
    }

    public sealed class StoreDiscoveryFeedbackPortFake : IStoreDiscoveryFeedbackPort
    {
        public WorkScheduled? StoredEvent { get; private set; }

        public Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken)
        {
            StoredEvent = @event;
            return Task.CompletedTask;
        }
    }
}

using Soundtrail.Contracts;
using Soundtrail.Contracts.Orchestrator.Commands;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;

public sealed record LookupSchedulingResult(
    LookupMusicCommand? Command)
{
    public bool ShouldSchedule => Command is not null;

    public static LookupSchedulingResult DoNotSchedule() => new(Command: null);

    public static LookupSchedulingResult Schedule(LookupMusicCommand command) => new(command);
}

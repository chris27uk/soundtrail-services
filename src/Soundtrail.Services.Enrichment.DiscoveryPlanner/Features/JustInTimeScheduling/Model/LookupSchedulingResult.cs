namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;

public sealed record LookupSchedulingResult(
    IReadOnlyList<LookupPhaseCommand> Commands)
{
    public bool ShouldSchedule => Commands.Count > 0;

    public LookupPhaseCommand? Command => Commands.SingleOrDefault();

    public static LookupSchedulingResult DoNotSchedule() => new([]);

    public static LookupSchedulingResult Schedule(params LookupPhaseCommand[] commands) => new(commands);
}

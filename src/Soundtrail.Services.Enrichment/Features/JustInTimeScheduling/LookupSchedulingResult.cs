namespace Soundtrail.Services.Enrichment.Features.JustInTimeScheduling;

public sealed record LookupSchedulingResult(
    LookupMusicCommand? Command)
{
    public bool ShouldSchedule => Command is not null;

    public static LookupSchedulingResult DoNotSchedule() => new(Command: null);

    public static LookupSchedulingResult Schedule(LookupMusicCommand command) => new(command);
}

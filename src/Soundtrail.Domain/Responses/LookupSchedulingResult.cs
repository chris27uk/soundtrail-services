using Soundtrail.Domain;
using Soundtrail.Domain.Commands;

namespace Soundtrail.Domain.Responses;

public sealed record LookupSchedulingResult(IReadOnlyList<ICommand> Commands)
{
    public bool ShouldSchedule => Commands.Count > 0;

    public LookupPhaseCommand? Command => Commands.OfType<LookupPhaseCommand>().SingleOrDefault();

    public static LookupSchedulingResult DoNotSchedule() => new([]);

    public static LookupSchedulingResult Schedule(params ICommand[] commands) => new(commands);
}

using Soundtrail.Services.Enrichment.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed record HighPriorityLookupMusicCommandMessage(LookupMusicCommand Command);

public sealed record LowPriorityLookupMusicCommandMessage(LookupMusicCommand Command);

internal static class LookupMusicCommandMessageExtensions
{
    public static object ToTransportMessage(this LookupMusicCommand command) =>
        command.Priority == LookupPriorityBand.High
            ? new HighPriorityLookupMusicCommandMessage(command)
            : new LowPriorityLookupMusicCommandMessage(command);
}

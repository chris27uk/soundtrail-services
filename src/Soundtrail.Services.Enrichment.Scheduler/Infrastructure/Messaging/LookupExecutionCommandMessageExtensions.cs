using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Queuing;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public static class LookupExecutionCommandMessageExtensions
{
    public static object ToMusicBrainzTransportMessage(this LookupMusicCommand command) =>
        command.Priority switch
        {
            LookupPriorityBand.High => new HighPriorityMusicBrainzLookupCommandMessage(command.ToExecutionCommand(ProviderName.MusicBrainz)),
            LookupPriorityBand.Low => new LowPriorityMusicBrainzLookupCommandMessage(command.ToExecutionCommand(ProviderName.MusicBrainz)),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Priority), command.Priority, "Unknown lookup priority.")
        };

    private static ExecuteLookupMusicCommand ToExecutionCommand(
        this LookupMusicCommand command,
        ProviderName provider)
    {
        return new ExecuteLookupMusicCommand(
            CommandId: CommandId.For($"{provider}:{command.MusicCatalogId.Value}"),
            MusicCatalogId: command.MusicCatalogId,
            Provider: provider,
            Priority: command.Priority,
            CreatedAt: command.CreatedAt,
            CorrelationId: command.CorrelationId);
    }
}

using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Queuing;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging
{
    internal static class LookupMusicCommandMessageExtensions
    {
        public static object ToTransportMessage(this LookupMusicCommand command) =>
            command.Priority == LookupPriorityBand.High
                ? new HighPriorityLookupMusicCommandMessage(command)
                : new LowPriorityLookupMusicCommandMessage(command);
    }
}
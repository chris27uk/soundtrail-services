using Soundtrail.Services.Enrichment.Shared.Queuing;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging
{
    public sealed record LowPriorityLookupMusicCommandMessage(LookupMusicCommand Command);
}
using Soundtrail.Services.Enrichment.Shared.Queuing;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging
{
    public sealed record LowPriorityLookupMusicCommandMessage(LookupMusicCommand Command);
}
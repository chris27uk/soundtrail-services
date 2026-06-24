using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.Scheduler.Features.SendDiscoveryBacklogMessage.Support;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.SendDiscoveryBacklogMessage.Adapters;

internal static class RunDiscoveryBacklogSchedulingCommandMappings
{
    public static object ToMessage(this object command) =>
        command switch
        {
            RunDiscoveryBacklogSchedulingCommand backlog => new RunDiscoveryBacklogSchedulingCommandDto(
                backlog.Now,
                backlog.Take),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
}

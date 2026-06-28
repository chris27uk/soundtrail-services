using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class RunDiscoveryBacklogSchedulingCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<RunDiscoveryBacklogSchedulingCommand, RunDiscoveryBacklogSchedulingCommandDto>(
            command =>
                new RunDiscoveryBacklogSchedulingCommandDto(
                    command.CreatedAt,
                    command.BatchSize),
            dto =>
                new RunDiscoveryBacklogSchedulingCommand(
                    CommandId.For($"RunDiscoveryBacklogScheduling:{dto.Now:O}:{dto.Take}"),
                    dto.Now,
                    CorrelationId.From($"RunDiscoveryBacklogScheduling:{dto.Now:O}:{dto.Take}"),
                    dto.Take));
    }
}

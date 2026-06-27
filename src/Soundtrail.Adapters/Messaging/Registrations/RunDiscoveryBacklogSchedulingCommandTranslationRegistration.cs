using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class RunDiscoveryBacklogSchedulingCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<RunDiscoveryBacklogSchedulingCommand, RunDiscoveryBacklogSchedulingCommandDto>(
            translate: command =>
                new RunDiscoveryBacklogSchedulingCommandDto(
                    command.CreatedAt,
                    command.BatchSize));
    }
}

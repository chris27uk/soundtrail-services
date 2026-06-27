using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class AssessMusicTrackCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<AssessMusicTrackCommand, AssessMusicTrackCommandDto>(
            translate: command =>
                new AssessMusicTrackCommandDto(
                    command.CommandId.Value,
                    command.CorrelationId.Value,
                    command.CreatedAt,
                    command.Priority,
                    command.MusicCatalogId.Value,
                    command.SearchTerm is null ? null : DiscoveryQueryKey.StableValueFor(command.SearchTerm),
                    command.TrustLevel,
                    command.RiskScore));
    }
}

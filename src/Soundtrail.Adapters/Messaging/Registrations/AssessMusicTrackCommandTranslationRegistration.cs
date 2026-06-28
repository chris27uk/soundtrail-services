using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class AssessMusicTrackCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<AssessMusicTrackCommand, AssessMusicTrackCommandDto>(
            command =>
                new AssessMusicTrackCommandDto(
                    command.CommandId.Value,
                    command.CorrelationId.Value,
                    command.CreatedAt,
                    command.Priority,
                    command.MusicCatalogId.Value,
                    command.SearchTerm is null ? null : DiscoveryQueryKey.StableValueFor(command.SearchTerm),
                    command.TrustLevel,
                    command.RiskScore),
            dto =>
                new AssessMusicTrackCommand(
                    CommandId.For(dto.CommandId),
                    CorrelationId.From(dto.CorrelationId),
                    dto.CreatedAt,
                    dto.Priority,
                    MusicCatalogId.From(dto.MusicCatalogId),
                    string.IsNullOrWhiteSpace(dto.Criteria) ? null : DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                    dto.TrustLevel,
                    dto.RiskScore));
    }
}
